using System.Net;
using System.Net.Http.Headers;
using AeonRegistryAPI.Enums;
using AeonRegistryAPI.IntegrationTests.Infrastructure;
using AeonRegistryAPI.IntegrationTests.TestData;
using AeonRegistryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AeonRegistryAPI.IntegrationTests.Api;

/// <summary>
/// Level 3 tests for <c>POST /api/private/artifacts/media-file/{artifactId}</c> - the last of
/// the endpoints that took its id from the query string.
/// </summary>
/// <remarks>
/// <para>
/// This one is worth having even though Artifacts are outside the deep slice, because it is the
/// only multipart endpoint in the app and the only place where the difference between a route
/// parameter and a query parameter is genuinely subtle: the handler also takes an
/// <see cref="IFormFile"/>, and a reader can reasonably assume the two scalars beside it come
/// from the form. They do not. Minimal APIs bind simple types from the route or the query string
/// and never from the form unless you ask with <c>[FromForm]</c>, so before the fix
/// <c>artifactId</c> was a query parameter on a multipart POST - which is legal, undocumented,
/// and not what anyone would guess from the signature.
/// </para>
/// <para>
/// <c>isPrimary</c> stays a query parameter after the fix, deliberately: it is a flag about how
/// to file the upload, not part of the resource's identity, so it has no place in the path.
/// </para>
/// </remarks>
public class ArtifactMediaFileEndpointsTests(AeonApiFactory factory)
    : IClassFixture<AeonApiFactory>, IAsyncLifetime
{
    // The eight-byte PNG signature. ImageValidationHelper reads the first eight bytes and
    // checks them against the magic numbers for JPEG/PNG/GIF/WEBP, so a valid header is all it
    // takes to get past validation - the payload does not have to decode as an image. That is
    // worth knowing rather than hiding: the helper validates the label, not the picture.
    private static readonly byte[] PngBytes =
        [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0x00, 0x00, 0x00, 0x0d];

    public async ValueTask InitializeAsync() => await factory.ResetDomainDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<int> SeedArtifactAsync()
    {
        await using var context = factory.CreateDbContext();

        var site = new SiteBuilder().Build();
        context.Sites.Add(site);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var artifact = new Artifact
        {
            Name = "Fluted Rod",
            CatalogNumber = "AEON-0001",
            DateDiscovered = new DateTime(2031, 4, 17, 0, 0, 0, DateTimeKind.Utc),
            Type = nameof(ArtifactType.Tool),
            SiteId = site.Id
        };

        context.Artifacts.Add(artifact);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return artifact.Id;
    }

    private static MultipartFormDataContent BuildImageContent()
    {
        var file = new ByteArrayContent(PngBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        // The form field name has to be "file" - it is matched against the handler's parameter
        // name, not positionally - and the file name has to carry an allowed extension, because
        // ImageValidationHelper checks it separately from the content type.
        return new MultipartFormDataContent { { file, "file", "rod.png" } };
    }

    [Fact]
    public async Task CreateArtifactMediaFile_WhenArtifactExists_ReturnsCreatedWithResolvableLocationHeader()
    {
        var artifactId = await SeedArtifactAsync();
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync(
            $"/api/private/artifacts/media-file/{artifactId}?isPrimary=true",
            BuildImageContent(),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Follow the Location header, as with the site create: it points into the *public*
        // image route, and a hand-built string crossing from the private surface to the public
        // one is the kind of thing that reads fine and resolves to nothing.
        var location = response.Headers.Location.ShouldNotBeNull();
        var followUp = await client.GetAsync(location, TestContext.Current.CancellationToken);
        followUp.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var context = factory.CreateDbContext();
        var stored = await context.ArtifactMediaFiles.SingleAsync(TestContext.Current.CancellationToken);
        stored.ArtifactId.ShouldBe(artifactId);
        stored.IsPrimary.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateArtifactMediaFile_WhenArtifactDoesNotExist_ReturnsNotFound()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync(
            "/api/private/artifacts/media-file/404?isPrimary=true",
            BuildImageContent(),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateArtifactMediaFile_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var artifactId = await SeedArtifactAsync();

        var response = await factory.CreateClient().PostAsync(
            $"/api/private/artifacts/media-file/{artifactId}?isPrimary=true",
            BuildImageContent(),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        await using var context = factory.CreateDbContext();
        (await context.ArtifactMediaFiles.AnyAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task CreateArtifactMediaFile_WhenArtifactIdIsOnlyInTheQueryString_ReturnsNotFound()
    {
        var artifactId = await SeedArtifactAsync();
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync(
            $"/api/private/artifacts/media-file?artifactId={artifactId}&isPrimary=true",
            BuildImageContent(),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        await using var context = factory.CreateDbContext();
        (await context.ArtifactMediaFiles.AnyAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }
}
