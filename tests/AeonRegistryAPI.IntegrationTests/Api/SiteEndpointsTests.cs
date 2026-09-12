using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AeonRegistryAPI.IntegrationTests.Infrastructure;
using AeonRegistryAPI.IntegrationTests.TestData;
using AeonRegistryAPI.Models.Request;
using AeonRegistryAPI.Models.Response;
using Microsoft.EntityFrameworkCore;

namespace AeonRegistryAPI.IntegrationTests.Api;

/// <summary>
/// Level 3 (host-level) tests for the site endpoints, over a real <see cref="HttpClient"/>.
/// </summary>
/// <remarks>
/// <para>
/// Everything asserted here is invisible to the service tests: which URL reaches which handler,
/// how <c>id</c> binds, whether <c>AddValidation()</c> is actually wired, what status code comes
/// back, what the JSON looks like on the wire, and whether the authorization policies do what
/// their names claim. Note what is <i>not</i> re-tested at this level - the field-by-field
/// mapping, the null-for-missing-row contract - because that is already covered a level down and
/// cheaper there.
/// </para>
/// <para>
/// The endpoint handlers themselves are <c>private static</c> methods on <c>SiteEndpoints</c>,
/// and they stay that way. Making them visible so they could be unit tested would buy
/// assertions about a method call while giving up the only thing that makes these tests worth
/// their runtime: the routing, binding, and filter pipeline in front of them.
/// </para>
/// <para>
/// The factory is a class fixture, so the host is built and the Identity users hashed once for
/// the whole class. Per-test isolation comes from <c>ResetDomainDataAsync</c> in
/// <see cref="InitializeAsync"/> instead.
/// </para>
/// </remarks>
public class SiteEndpointsTests(AeonApiFactory factory) : IClassFixture<AeonApiFactory>, IAsyncLifetime
{
    private const string ArchivedSuffix = " [ARCHIVED]";

    public async ValueTask InitializeAsync() => await factory.ResetDomainDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<int> SeedSiteAsync(SiteBuilder builder)
    {
        await using var context = factory.CreateDbContext();

        var site = builder.Build();
        context.Sites.Add(site);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return site.Id;
    }

    // ---------------------------------------------------------------------------------------
    // GET /api/public/sites
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task GetPublicSites_WhenUnauthenticated_ReturnsOk()
    {
        await SeedSiteAsync(new SiteBuilder().WithName("Ashfall Terrace"));

        var response = await factory.CreateClient()
            .GetAsync("/api/public/sites", TestContext.Current.CancellationToken);

        // No token attached, on purpose: the public group has no RequireAuthorization, and an
        // accidental one on it would break every anonymous consumer - including
        // wwwroot/site/sites-map.js.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPublicSites_WhenSiteHasAeonNarrative_OmitsItFromTheResponseBody()
    {
        await SeedSiteAsync(new SiteBuilder()
            .WithName("Ashfall Terrace")
            .WithAeonNarrative("Restricted: do not publish."));

        var body = await factory.CreateClient()
            .GetFromJsonAsync<JsonNode>("/api/public/sites", TestContext.Current.CancellationToken);

        // Asserted against the raw JSON rather than a deserialized PublicSiteResponse, because
        // deserializing into the public DTO would discard an aeonNarrative key even if the API
        // sent one - the test would pass precisely when it should fail. This is the leak the
        // public/private split exists to prevent, so it gets checked on the wire.
        var site = body.ShouldNotBeNull().AsArray().ShouldHaveSingleItem()!.AsObject();
        site["name"]!.GetValue<string>().ShouldBe("Ashfall Terrace");
        site.ContainsKey("aeonNarrative").ShouldBeFalse();
    }

    // ---------------------------------------------------------------------------------------
    // GET /api/public/sites/{id}
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task GetPublicSiteById_WhenSiteExists_ReturnsOkWithTheSite()
    {
        var siteId = await SeedSiteAsync(new SiteBuilder().WithName("Glasswater Basin"));

        var response = await factory.CreateClient()
            .GetAsync($"/api/public/sites/{siteId}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var site = await response.Content.ReadFromJsonAsync<PublicSiteResponse>(
            TestContext.Current.CancellationToken);
        site.ShouldNotBeNull();
        site.Id.ShouldBe(siteId);
        site.Name.ShouldBe("Glasswater Basin");
    }

    [Fact]
    public async Task GetPublicSiteById_WhenSiteDoesNotExist_ReturnsNotFound()
    {
        var response = await factory.CreateClient()
            .GetAsync("/api/public/sites/404", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPublicSiteById_WhenIdIsNotAnInteger_ReturnsNotFound()
    {
        var response = await factory.CreateClient()
            .GetAsync("/api/public/sites/not-a-number", TestContext.Current.CancellationToken);

        // The {id:int} route constraint, doing its job: a non-numeric id fails to match the
        // route at all, so routing answers 404 and the handler never runs. Without the
        // constraint this would be a 400 from model binding - a different contract, and one
        // worth pinning down rather than discovering later.
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ---------------------------------------------------------------------------------------
    // GET /api/private/sites - the authentication boundary
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task GetPrivateSites_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient()
            .GetAsync("/api/private/sites", TestContext.Current.CancellationToken);

        // 401, not 404: the route exists, the caller just has not identified themselves.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPrivateSites_WhenAuthenticated_ReturnsOkWithAeonNarrative()
    {
        await SeedSiteAsync(new SiteBuilder().WithAeonNarrative("Resonance readings unexplained."));

        var client = await factory.CreateAuthenticatedClientAsync();
        var body = await client.GetFromJsonAsync<JsonNode>(
            "/api/private/sites", TestContext.Current.CancellationToken);

        // The mirror of the public test: a real login, and the staff-only field is present. Any
        // user will do here - the private group requires authentication but no particular role.
        var site = body.ShouldNotBeNull().AsArray().ShouldHaveSingleItem()!.AsObject();
        site["aeonNarrative"]!.GetValue<string>().ShouldBe("Resonance readings unexplained.");
    }

    // ---------------------------------------------------------------------------------------
    // POST /api/private/sites
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task CreatePrivateSite_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/private/sites",
            new SiteBuilder().BuildCreateRequest(),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // And nothing was written. A 401 that still persisted the row would be a far worse bug
        // than a wrong status code.
        await using var context = factory.CreateDbContext();
        (await context.Sites.AnyAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task CreatePrivateSite_WithValidRequest_ReturnsCreatedWithResolvableLocationHeader()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var request = new SiteBuilder().WithName("Glasswater Basin").BuildCreateRequest();

        var response = await client.PostAsJsonAsync(
            "/api/private/sites", request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var location = response.Headers.Location.ShouldNotBeNull();

        // The Location header is only useful if it actually resolves, so follow it. A hand-built
        // URL string - which is what the handler does - is easy to get subtly wrong (a missing
        // segment, a stale route prefix) and impossible to notice by reading it.
        var followUp = await client.GetAsync(location, TestContext.Current.CancellationToken);
        followUp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var site = await followUp.Content.ReadFromJsonAsync<PrivateSiteResponse>(
            TestContext.Current.CancellationToken);
        site.ShouldNotBeNull().Name.ShouldBe("Glasswater Basin");
    }

    [Fact]
    public async Task CreatePrivateSite_WhenNameIsMissing_ReturnsValidationProblem()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        // Deliberately not a CreateSiteRequest: an anonymous object is the only way to send a
        // body with the property genuinely absent, which is what a real broken client sends.
        var response = await client.PostAsJsonAsync(
            "/api/private/sites",
            new { location = "Northern Reach, Sector 12" },
            TestContext.Current.CancellationToken);

        // This is the test that proves AddValidation() is wired at all. Without it, [Required]
        // on the DTO is inert in a minimal API and this request would reach the service and be
        // persisted with a null name.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<JsonNode>(
            TestContext.Current.CancellationToken);
        var errors = problem.ShouldNotBeNull()["errors"].ShouldNotBeNull().AsObject();
        errors.Select(e => e.Key).ShouldContain(nameof(CreateSiteRequest.Name));

        await using var context = factory.CreateDbContext();
        (await context.Sites.AnyAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    // ---------------------------------------------------------------------------------------
    // POST /api/private/sites/{id}/archive/ - the authorization example
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task ArchiveSite_WhenCallerIsNotAnArchivist_ReturnsForbidden()
    {
        var siteId = await SeedSiteAsync(new SiteBuilder().WithName("Ashfall Terrace"));
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync(
            $"/api/private/sites/{siteId}/archive/", content: null, TestContext.Current.CancellationToken);

        // 403, not 401: this caller is authenticated, they are simply not allowed. Confusing the
        // two is a real bug - a 401 tells a client to log in again, which would send this user
        // round a login loop that can never succeed.
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // And the site is untouched, which is what makes the status code more than cosmetic.
        await using var context = factory.CreateDbContext();
        var stored = await context.Sites.SingleAsync(s => s.Id == siteId, TestContext.Current.CancellationToken);
        stored.Name.ShouldBe("Ashfall Terrace");
    }

    [Fact]
    public async Task ArchiveSite_WhenCallerIsAnArchivist_ReturnsNoContentAndArchivesTheSite()
    {
        var siteId = await SeedSiteAsync(new SiteBuilder().WithName("Ashfall Terrace"));
        var client = await factory.CreateAuthenticatedClientAsync(AeonApiFactory.ArchivistRole);

        var response = await client.PostAsync(
            $"/api/private/sites/{siteId}/archive/", content: null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // This pair of tests is the reason the auth helper logs in for real instead of stubbing a
        // principal. Everything between "this user has the Archivist role" and "the policy lets
        // the request through" - the role claim being written into the sign-in, carried by the
        // token, and read back on the next request - is exercised here, and none of it would be
        // if the identity were faked.
        await using var context = factory.CreateDbContext();
        var stored = await context.Sites.SingleAsync(s => s.Id == siteId, TestContext.Current.CancellationToken);
        stored.Name.ShouldBe($"Ashfall Terrace{ArchivedSuffix}");
    }

    [Fact]
    public async Task ArchiveSite_WhenSiteDoesNotExist_ReturnsNotFound()
    {
        var client = await factory.CreateAuthenticatedClientAsync(AeonApiFactory.ArchivistRole);

        var response = await client.PostAsync(
            "/api/private/sites/404/archive/", content: null, TestContext.Current.CancellationToken);

        // Authorization is evaluated before the handler, so a missing site only surfaces once the
        // caller is allowed in - an unauthorized caller gets 403 whether the site exists or not,
        // which is the right order for not leaking which ids are real.
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ---------------------------------------------------------------------------------------
    // PUT /api/private/sites/{id}
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task UpdatePrivateSite_WhenSiteExists_ReturnsNoContentAndUpdatesTheSite()
    {
        var siteId = await SeedSiteAsync(new SiteBuilder().WithName("Ashfall Terrace"));
        var client = await factory.CreateAuthenticatedClientAsync();
        var request = new SiteBuilder().WithName("Ashfall Terrace (Lower)").BuildUpdateRequest();

        var response = await client.PutAsJsonAsync(
            $"/api/private/sites/{siteId}", request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await using var context = factory.CreateDbContext();
        var stored = await context.Sites.SingleAsync(s => s.Id == siteId, TestContext.Current.CancellationToken);
        stored.Name.ShouldBe("Ashfall Terrace (Lower)");
    }

    [Fact]
    public async Task UpdatePrivateSite_WhenSiteDoesNotExist_ReturnsNotFound()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync(
            "/api/private/sites/404",
            new SiteBuilder().BuildUpdateRequest(),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePrivateSite_WhenIdIsOnlyInTheQueryString_ReturnsMethodNotAllowed()
    {
        var siteId = await SeedSiteAsync(new SiteBuilder().WithName("Ashfall Terrace"));
        var client = await factory.CreateAuthenticatedClientAsync();
        var request = new SiteBuilder().WithName("Renamed via a query string").BuildUpdateRequest();

        var response = await client.PutAsJsonAsync(
            $"/api/private/sites?id={siteId}", request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);

        await using var context = factory.CreateDbContext();
        var stored = await context.Sites.SingleAsync(s => s.Id == siteId, TestContext.Current.CancellationToken);
        stored.Name.ShouldBe("Ashfall Terrace");
    }

    // ---------------------------------------------------------------------------------------
    // DELETE /api/private/sites/{id}
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task DeletePrivateSite_WhenSiteExists_ReturnsNoContentAndRemovesTheSite()
    {
        var siteId = await SeedSiteAsync(new SiteBuilder().WithName("Ashfall Terrace"));
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync(
            $"/api/private/sites/{siteId}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await using var context = factory.CreateDbContext();
        (await context.Sites.AnyAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeletePrivateSite_WhenSiteDoesNotExist_ReturnsNotFound()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync(
            "/api/private/sites/404", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePrivateSite_WhenIdIsOnlyInTheQueryString_ReturnsMethodNotAllowed()
    {
        var siteId = await SeedSiteAsync(new SiteBuilder());
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync(
            $"/api/private/sites?id={siteId}", TestContext.Current.CancellationToken);

        // A deletion that answers to a URL nobody documented is the worst version of this bug,
        // so the retirement of that URL is asserted on the data as well as the status code.
        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);

        await using var context = factory.CreateDbContext();
        (await context.Sites.AnyAsync(TestContext.Current.CancellationToken)).ShouldBeTrue();
    }
}
