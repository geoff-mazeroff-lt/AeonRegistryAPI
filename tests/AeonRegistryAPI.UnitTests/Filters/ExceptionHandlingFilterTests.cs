using AeonRegistryAPI.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace AeonRegistryAPI.UnitTests.Filters;

/// <summary>
/// Level 1 (unit) tests for <see cref="ExceptionHandlingFilter"/>.
/// </summary>
/// <remarks>
/// <para>
/// An endpoint filter is testable without a host because its whole contract is one method:
/// take a context and the next delegate, return an object. <c>DefaultHttpContext</c> supplies
/// the context and a hand-written <see cref="EndpointFilterDelegate"/> supplies the "rest of
/// the pipeline", so no routing, no server, and no request are involved.
/// </para>
/// <para>
/// This filter resolves <see cref="IWebHostEnvironment"/> from
/// <c>HttpContext.RequestServices</c> rather than taking it as a constructor parameter. That
/// is service location, and it is the one thing here that makes the test more work than it
/// needs to be: the dependency has to be smuggled in through a container instead of just
/// passed. Worth noticing - constructor injection would have made this a two-line arrange.
/// </para>
/// </remarks>
public class ExceptionHandlingFilterTests
{
    private readonly ExceptionHandlingFilter _filter = new();

    /// <summary>
    /// Builds a context whose <c>RequestServices</c> can resolve an
    /// <see cref="IWebHostEnvironment"/> reporting the given environment name.
    /// </summary>
    /// <remarks>
    /// <c>IsDevelopment()</c> is an extension method, so it cannot be substituted directly.
    /// It reads <c>EnvironmentName</c>, so that is the property to configure - a recurring
    /// gotcha with NSubstitute and the hosting abstractions.
    /// </remarks>
    private static EndpointFilterInvocationContext CreateContext(string environmentName)
    {
        var environment = Substitute.For<IWebHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);

        var services = new ServiceCollection();
        services.AddSingleton(environment);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        return EndpointFilterInvocationContext.Create(httpContext);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextSucceeds_ReturnsResultUnchanged()
    {
        var context = CreateContext(Environments.Production);
        var expected = Results.Ok("the artifact");

        var result = await _filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>(expected));

        // Reference equality on purpose: the filter must not wrap, copy, or reinterpret a
        // successful result.
        result.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextSucceeds_CallsNextExactlyOnce()
    {
        var context = CreateContext(Environments.Production);
        var callCount = 0;

        await _filter.InvokeAsync(context, _ =>
        {
            callCount++;
            return ValueTask.FromResult<object?>(Results.NoContent());
        });

        // A spy rather than an NSubstitute mock: EndpointFilterDelegate is a delegate type,
        // and counting invocations of a closure is both shorter and clearer than substituting
        // one. Reach for NSubstitute when the collaborator is an interface with behavior.
        callCount.ShouldBe(1);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrowsInAnyEnvironment_ReturnsProblemWithStatusFiveHundred()
    {
        var context = CreateContext(Environments.Production);

        var result = await _filter.InvokeAsync(context, _ => throw new InvalidOperationException("boom"));

        var problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        problem.ProblemDetails.Status.ShouldBe(StatusCodes.Status500InternalServerError);

        problem.ProblemDetails.Title.ShouldBe("An unexpected error occurred");
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrowsAsynchronouslyInAnyEnvironment_ReturnsProblem()
    {
        var context = CreateContext(Environments.Production);

        // The previous test throws before the delegate's ValueTask is created; this one throws
        // inside an awaited task, so the exception arrives at the `await next(context)` line
        // instead. Both paths reach the same catch, and this test is what proves it.
        var result = await _filter.InvokeAsync(context, async _ =>
        {
            await Task.Yield();
            throw new InvalidOperationException("boom");
        });

        result.ShouldBeOfType<ProblemHttpResult>()
            .StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrowsInDevelopment_IncludesExceptionDetail()
    {
        var context = CreateContext(Environments.Development);

        var result = await _filter.InvokeAsync(
            context, _ => throw new InvalidOperationException("narrative decode failed"));

        var problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.ProblemDetails.Detail.ShouldNotBeNull();
        problem.ProblemDetails.Detail.ShouldContain("narrative decode failed");
        problem.ProblemDetails.Detail.ShouldContain(nameof(InvalidOperationException));
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Testing")] // the environment the API and contract tests run under
    public async Task InvokeAsync_WhenNextThrowsOutsideDevelopment_OmitsExceptionDetail(string environmentName)
    {
        var context = CreateContext(environmentName);

        var result = await _filter.InvokeAsync(
            context, _ => throw new InvalidOperationException("narrative decode failed"));

        // The point of the whole filter: a stack trace is a debugging aid in Development and
        // an information leak anywhere else.
        result.ShouldBeOfType<ProblemHttpResult>()
            .ProblemDetails.Detail.ShouldBeNull();
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_DoesNotRethrow()
    {
        var context = CreateContext(Environments.Production);

        // Stated as its own test because it is the filter's reason for existing: the request
        // must not fail with an unhandled exception, whatever the endpoint does.
        await Should.NotThrowAsync(async () =>
            await _filter.InvokeAsync(context, _ => throw new OutOfMemoryException()));
    }
}
