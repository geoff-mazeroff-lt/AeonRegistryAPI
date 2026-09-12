using System.Net;
using System.Net.Http.Json;
using AeonRegistryAPI.Enums;
using AeonRegistryAPI.IntegrationTests.Infrastructure;
using AeonRegistryAPI.IntegrationTests.TestData;
using AeonRegistryAPI.Models;
using AeonRegistryAPI.Models.Request;
using Microsoft.EntityFrameworkCore;

namespace AeonRegistryAPI.IntegrationTests.Api;

/// <summary>
/// Level 3 tests for the one artifact endpoint whose route shape was wrong:
/// <c>PUT /api/private/artifacts/{id}</c>.
/// </summary>
/// <remarks>
/// <para>
/// Artifacts are deliberately outside the deep slice - Sites is the worked example, and
/// extending the same four levels to Artifacts is the exercise left for the team. This class is
/// the exception because the route bug it covers is the same bug as on Sites, and fixing one
/// while leaving the other would leave the convention inconsistent in the very surface the
/// contract snapshot is about to baseline.
/// </para>
/// <para>
/// Note what that scope decision costs: there is no <c>ArtifactBuilder</c> to match
/// <see cref="SiteBuilder"/>, so the seeding helper below constructs the entity inline. That is
/// the right trade at two call sites and the wrong one at ten - when the Artifacts slice gets
/// built out, the first step is to promote this helper into a builder in <c>TestData/</c>.
/// </para>
/// </remarks>
public class ArtifactEndpointsTests(AeonApiFactory factory) : IClassFixture<AeonApiFactory>, IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await factory.ResetDomainDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<(int SiteId, int ArtifactId)> SeedArtifactAsync(string name)
    {
        await using var context = factory.CreateDbContext();

        var site = new SiteBuilder().Build();
        context.Sites.Add(site);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var artifact = new Artifact
        {
            Name = name,
            CatalogNumber = "AEON-0001",
            Description = "A fluted rod of unknown alloy.",
            PublicNarrative = "On display in the east gallery.",
            DateDiscovered = new DateTime(2031, 4, 17, 0, 0, 0, DateTimeKind.Utc),
            Type = nameof(ArtifactType.Tool),
            SiteId = site.Id
        };

        context.Artifacts.Add(artifact);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (site.Id, artifact.Id);
    }

    private static UpdateArtifactRequest BuildUpdateRequest(int siteId, string name) => new()
    {
        Name = name,
        CatalogNumber = "AEON-0001",
        Description = "A fluted rod of unknown alloy.",
        PublicNarrative = "On display in the east gallery.",
        DateDiscovered = new DateTime(2031, 4, 17, 0, 0, 0, DateTimeKind.Utc),
        Type = nameof(ArtifactType.Tool),
        SiteId = siteId
    };

    // ---------------------------------------------------------------------------------------
    // PUT /api/private/artifacts/{id}
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task UpdatePrivateArtifact_WhenArtifactExists_ReturnsNoContentAndUpdatesTheArtifact()
    {
        var (siteId, artifactId) = await SeedArtifactAsync("Fluted Rod");
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/private/artifacts/{artifactId}",
            BuildUpdateRequest(siteId, "Fluted Rod (Fragment A)"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await using var context = factory.CreateDbContext();
        var stored = await context.Artifacts.SingleAsync(
            a => a.Id == artifactId, TestContext.Current.CancellationToken);
        stored.Name.ShouldBe("Fluted Rod (Fragment A)");
    }

    [Fact]
    public async Task UpdatePrivateArtifact_WhenArtifactDoesNotExist_ReturnsNotFound()
    {
        var (siteId, _) = await SeedArtifactAsync("Fluted Rod");
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync(
            "/api/private/artifacts/404",
            BuildUpdateRequest(siteId, "Fluted Rod"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePrivateArtifact_WhenIdIsOnlyInTheQueryString_ReturnsMethodNotAllowed()
    {
        var (siteId, artifactId) = await SeedArtifactAsync("Fluted Rod");
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/private/artifacts?id={artifactId}",
            BuildUpdateRequest(siteId, "Renamed via a query string"),
            TestContext.Current.CancellationToken);

        // The old form retired. 405, not 404: /api/private/artifacts still answers GET and
        // POST, so the matcher finds the route and rejects only the verb.
        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);

        await using var context = factory.CreateDbContext();
        var stored = await context.Artifacts.SingleAsync(
            a => a.Id == artifactId, TestContext.Current.CancellationToken);
        stored.Name.ShouldBe("Fluted Rod");
    }
}
