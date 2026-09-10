using System.Text.Json;
using AeonRegistryAPI.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AeonRegistryAPI.UnitTests.Middleware;

/// <summary>
/// Level 1 (unit) tests for <see cref="BlockIdentityEndpoints"/>.
/// </summary>
/// <remarks>
/// Middleware is a good unit-test target for the same reason an endpoint filter is: the
/// contract is one method over an <see cref="HttpContext"/>. A <c>DefaultHttpContext</c> plus a
/// <see cref="RequestDelegate"/> spy covers both outcomes - short-circuit with a 404, or hand
/// off to the rest of the pipeline - with no server and no routing involved.
///
/// What this level cannot tell us is whether the middleware is registered in the right place
/// in <c>UseApplicationPipeline()</c>; only an API-level test can. Worth being explicit about:
/// every one of these tests can pass while the middleware never runs in production.
/// </remarks>
public class BlockIdentityEndpointsTests
{
    /// <summary>Records whether the rest of the pipeline was reached.</summary>
    private sealed class NextSpy
    {
        public int CallCount { get; private set; }
        public bool WasCalled => CallCount > 0;

        public Task Invoke(HttpContext context)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private static DefaultHttpContext CreateContext(string path) =>
        new()
        {
            Request = { Path = path },
            // A readable body, so the 404 payload can be asserted rather than assumed.
            Response = { Body = new MemoryStream() },
            // WriteAsJsonAsync looks for JsonOptions here and falls back to defaults; an empty
            // provider keeps that lookup from depending on ambient state.
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };

    private static async Task<(DefaultHttpContext Context, NextSpy Next)> InvokeAsync(string path)
    {
        var context = CreateContext(path);
        var next = new NextSpy();

        await new BlockIdentityEndpoints(next.Invoke).InvokeAsync(context);

        return (context, next);
    }

    // ---------------------------------------------------------------------------------
    // Blocked paths
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("/api/auth/register")]
    [InlineData("/api/auth/refresh")]
    [InlineData("/api/auth/confirmemail")]
    [InlineData("/api/auth/resendconfirmationemail")]
    [InlineData("/api/auth/forgotpassword")]
    [InlineData("/api/auth/resetpassword")]
    [InlineData("/api/auth/manage/info")]
    [InlineData("/api/auth/manage/2fa")]
    [InlineData("/api/auth/manage/profile")]
    public async Task InvokeAsync_BlockedPath_RespondsNotFoundAndShortCircuits(string path)
    {
        var (context, next) = await InvokeAsync(path);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);

        // The more important half of the assertion: a 404 status with the request still
        // flowing to the Identity endpoint underneath would block nothing at all.
        next.WasCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task InvokeAsync_BlockedPath_WritesJsonMessageNamingThePath()
    {
        var (context, _) = await InvokeAsync("/api/auth/register");

        context.Response.ContentType.ShouldBe("application/json; charset=utf-8");

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(
            context.Response.Body, cancellationToken: TestContext.Current.CancellationToken);

        // camelCase because WriteAsJsonAsync uses the web defaults, even though the anonymous
        // type declares `Message`. Pinning it is the point: the casing is what clients see.
        document.RootElement.GetProperty("message").GetString()
            .ShouldBe("Endpoint '/api/auth/register' is disabled.");
    }

    [Theory]
    [InlineData("/API/AUTH/REGISTER")]
    [InlineData("/Api/Auth/Register")]
    [InlineData("/api/auth/MANAGE/Info")]
    [InlineData("/api/auth/ResetPassword")]
    public async Task InvokeAsync_BlockedPathInDifferentCasing_RespondsNotFound(string path)
    {
        // Routing treats URL paths as case-insensitive, so a block list that compared
        // case-sensitively would be trivially bypassable. `ToLowerInvariant()` already handles
        // this - the test exists to keep it handled, since nothing else in the file explains
        // why that call is there.
        var (context, next) = await InvokeAsync(path);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        next.WasCalled.ShouldBeFalse();
    }

    [Theory]
    [InlineData("/api/auth/register/")]
    [InlineData("/api/auth/forgotpassword/")]
    [InlineData("/API/Auth/Register/")]
    [InlineData("/api/auth/manage/info/")]
    public async Task InvokeAsync_BlockedPathWithTrailingSlash_RespondsNotFound(string path)
    {
        var (context, next) = await InvokeAsync(path);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        next.WasCalled.ShouldBeFalse();
    }

    // ---------------------------------------------------------------------------------
    // Pass-through
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("/api/public/sites")]
    [InlineData("/api/private/sites")]
    [InlineData("/site/sites-map.html")]
    [InlineData("/swagger/index.html")]
    [InlineData("/")]
    public async Task InvokeAsync_UnblockedPath_CallsNext(string path)
    {
        var (context, next) = await InvokeAsync(path);

        next.CallCount.ShouldBe(1);

        // 200 is DefaultHttpContext's initial value: the assertion is that the middleware left
        // the response alone, not that anything set it to 200.
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Theory]
    [InlineData("/api/auth/login")] // the one built-in Identity route this API keeps
    [InlineData("/api/public/auth/register-admin")]
    [InlineData("/api/public/auth/forgot-password")]
    [InlineData("/api/public/auth/reset-password")]
    [InlineData("/api/private/auth/manage-profile")]
    [InlineData("/api/private/auth/manage/users")]
    public async Task InvokeAsync_LoginOrCustomIdentityEndpoint_CallsNext(string path)
    {
        // The replacements in CustomIdentityEndpoints must stay reachable. They live under
        // /api/public/auth and /api/private/auth - not /api/auth, which is the group
        // MapIdentityApi owns - so nothing on the block list resembles them today. The test
        // guards against a future edit broadening the matching to a prefix.
        var (_, next) = await InvokeAsync(path);

        next.WasCalled.ShouldBeTrue();
    }

    [Theory]
    [InlineData("/api/auth/registered")]
    [InlineData("/api/auth/register/extra")]
    [InlineData("/prefix/api/auth/register")]
    public async Task InvokeAsync_PathMerelyResemblingABlockedPath_CallsNext(string path)
    {
        // Matching is exact by design: blocking every path that contains a
        // blocked one would take out unrelated routes. These three are the neighbors of
        // "/api/auth/register" that must stay reachable.
        var (_, next) = await InvokeAsync(path);

        next.WasCalled.ShouldBeTrue();
    }
}
