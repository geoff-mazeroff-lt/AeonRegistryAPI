using AeonRegistryAPI.IntegrationTests.Infrastructure;
using AeonRegistryAPI.IntegrationTests.TestData;
using AeonRegistryAPI.Models.Response;
using Microsoft.EntityFrameworkCore;

// AeonRegistryAPI.Services.Site is a namespace and AeonRegistryAPI.Models.Site is a type, so
// importing both would make the bare name `Site` ambiguous. Aliasing the one type this file
// needs by name is the least surprising way out; the entity itself only ever appears here as
// the `var` returned by SiteBuilder.
using SiteService = AeonRegistryAPI.Services.Site.SiteService;

namespace AeonRegistryAPI.IntegrationTests.Services;

/// <summary>
/// Level 2 (service + EF) tests for <c>SiteService</c>, run against SQLite in-memory.
/// </summary>
/// <remarks>
/// <para>
/// These are not unit tests and cannot be: <c>SiteService</c> depends on the concrete
/// <c>ApplicationDbContext</c>, so there is nothing to substitute. That is why
/// NSubstitute never appears in this file. What the database buys in exchange is the ability
/// to catch the things that only exist once EF is involved - LINQ that does not translate,
/// projections that drop or mis-map a column, and whether <c>SaveChangesAsync</c> actually
/// wrote what the method claims it wrote.
/// </para>
/// <para>
/// <b>Isolation:</b> a fresh database per test. xUnit constructs a new instance of this class
/// for every test method, so the <see cref="SqliteDatabaseFixture"/> field below is built and
/// torn down once per test - and in return no test can see another
/// test's rows or depend on running order. Sharing one database across the class via
/// <c>IClassFixture</c> would be faster and would reintroduce exactly that flakiness.
/// Wrapping each test in a rolled-back transaction is the other well-known option; it is
/// faster still, but it breaks down as soon as the code under test manages its own
/// transaction, so it is worth knowing about rather than reaching for.
/// </para>
/// <para>
/// <b>Every test uses one context to arrange, a second for the service, and a third to
/// assert.</b> This is deliberate and it is the habit to take away from this file. EF's change
/// tracker will answer a query from memory when the entity is already loaded, so a test that
/// asserts through its arrange context can pass while the database is empty.
/// </para>
/// </remarks>
public class SiteServiceTests : IAsyncLifetime
{
    private const string ArchivedSuffix = " [ARCHIVED]";

    private readonly SqliteDatabaseFixture _database = new();

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _database.DisposeAsync();

    /// <summary>Inserts a site through its own context and returns its database-assigned id.</summary>
    private async Task<int> SeedSiteAsync(SiteBuilder builder)
    {
        await using var arrangeContext = _database.CreateContext();

        var site = builder.Build();
        arrangeContext.Sites.Add(site);
        await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return site.Id;
    }

    // ---------------------------------------------------------------------------------------
    // GetAllPublicSitesAsync
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task GetAllPublicSitesAsync_WhenSitesExist_ReturnsEverySite()
    {
        await SeedSiteAsync(new SiteBuilder().WithName("Ashfall Terrace"));
        await SeedSiteAsync(new SiteBuilder().WithName("Glasswater Basin"));

        await using var actContext = _database.CreateContext();
        var sites = await new SiteService(actContext)
            .GetAllPublicSitesAsync(TestContext.Current.CancellationToken);

        sites.Select(s => s.Name).ShouldBe(["Ashfall Terrace", "Glasswater Basin"], ignoreOrder: true);
    }

    [Fact]
    public async Task GetAllPublicSitesAsync_WhenNoSitesExist_ReturnsEmptyCollection()
    {
        await using var actContext = _database.CreateContext();

        var sites = await new SiteService(actContext)
            .GetAllPublicSitesAsync(TestContext.Current.CancellationToken);

        // Empty, not null. An endpoint returning null here would serialize as `null` instead of
        // `[]` and break every caller that iterates the response.
        sites.ShouldBeEmpty();
    }

    // ---------------------------------------------------------------------------------------
    // GetPublicSiteByIdAsync
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task GetPublicSiteByIdAsync_WhenSiteExists_ReturnsTheProjectedSite()
    {
        var siteId = await SeedSiteAsync(new SiteBuilder()
            .WithName("Ashfall Terrace")
            .WithLocation("Northern Reach, Sector 12")
            .WithCoordinates("64.1466° N, 21.9426° W")
            .WithPosition(64.1466, -21.9426)
            .WithDescription("A terraced excavation cut into volcanic ash.")
            .WithPublicNarrative("Visitors may view the lower terrace."));

        await using var actContext = _database.CreateContext();
        var site = await new SiteService(actContext)
            .GetPublicSiteByIdAsync(siteId, TestContext.Current.CancellationToken);

        site.ShouldNotBeNull();
        site.Id.ShouldBe(siteId);
        site.Name.ShouldBe("Ashfall Terrace");
        site.Location.ShouldBe("Northern Reach, Sector 12");
        site.Coordinates.ShouldBe("64.1466° N, 21.9426° W");
        site.Latitude.ShouldBe(64.1466);
        site.Longitude.ShouldBe(-21.9426);
        site.Description.ShouldBe("A terraced excavation cut into volcanic ash.");
        site.PublicNarrative.ShouldBe("Visitors may view the lower terrace.");
    }

    [Fact]
    public async Task GetPublicSiteByIdAsync_WhenSiteDoesNotExist_ReturnsNull()
    {
        await using var actContext = _database.CreateContext();

        var site = await new SiteService(actContext)
            .GetPublicSiteByIdAsync(404, TestContext.Current.CancellationToken);

        // Null rather than an exception is the contract the endpoint relies on to return a 404.
        site.ShouldBeNull();
    }

    // ---------------------------------------------------------------------------------------
    // GetAllPrivateSitesAsync / GetPrivateSiteByIdAsync
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task GetAllPrivateSitesAsync_WhenSitesExist_IncludesAeonNarrative()
    {
        await SeedSiteAsync(new SiteBuilder().WithAeonNarrative("Resonance readings unexplained."));

        await using var actContext = _database.CreateContext();
        var sites = await new SiteService(actContext)
            .GetAllPrivateSitesAsync(TestContext.Current.CancellationToken);

        sites.ShouldHaveSingleItem()
            .AeonNarrative.ShouldBe("Resonance readings unexplained.");
    }

    [Fact]
    public async Task GetPrivateSiteByIdAsync_WhenSiteExists_IncludesAeonNarrative()
    {
        var siteId = await SeedSiteAsync(new SiteBuilder()
            .WithName("Ashfall Terrace")
            .WithAeonNarrative("Resonance readings unexplained."));

        await using var actContext = _database.CreateContext();
        var site = await new SiteService(actContext)
            .GetPrivateSiteByIdAsync(siteId, TestContext.Current.CancellationToken);

        site.ShouldNotBeNull();
        site.Name.ShouldBe("Ashfall Terrace");
        site.AeonNarrative.ShouldBe("Resonance readings unexplained.");
    }

    [Fact]
    public async Task GetPrivateSiteByIdAsync_WhenSiteDoesNotExist_ReturnsNull()
    {
        await using var actContext = _database.CreateContext();

        var site = await new SiteService(actContext)
            .GetPrivateSiteByIdAsync(404, TestContext.Current.CancellationToken);

        site.ShouldBeNull();
    }

    // ---------------------------------------------------------------------------------------
    // CreateSiteAsync
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task CreateSiteAsync_WithValidRequest_PersistsTheSite()
    {
        var request = new SiteBuilder()
            .WithName("Glasswater Basin")
            .WithLocation("Southern Shelf, Sector 3")
            .WithAeonNarrative("Restricted: sediment cores withheld.")
            .BuildCreateRequest();

        await using var actContext = _database.CreateContext();
        await new SiteService(actContext).CreateSiteAsync(request, TestContext.Current.CancellationToken);

        // The assertion runs on a context that has never seen this entity, so it has to read the
        // row back out of the database. Reusing `actContext` here would pass even if
        // SaveChangesAsync were never called.
        await using var assertContext = _database.CreateContext();
        var stored = await assertContext.Sites.SingleAsync(TestContext.Current.CancellationToken);
        stored.Name.ShouldBe("Glasswater Basin");
        stored.Location.ShouldBe("Southern Shelf, Sector 3");
        stored.AeonNarrative.ShouldBe("Restricted: sediment cores withheld.");
    }

    [Fact]
    public async Task CreateSiteAsync_WithValidRequest_ReturnsDatabaseAssignedId()
    {
        var request = new SiteBuilder().BuildCreateRequest();

        await using var actContext = _database.CreateContext();
        var created = await new SiteService(actContext)
            .CreateSiteAsync(request, TestContext.Current.CancellationToken);

        // The id is generated by the database, so it only exists after SaveChangesAsync; EF
        // writes it back onto the tracked entity. The endpoint uses this value to build the
        // 201 Location header, which is why "did it come back at all" is worth its own test.
        created.Id.ShouldBeGreaterThan(0);

        await using var assertContext = _database.CreateContext();
        var stored = await assertContext.Sites.SingleAsync(TestContext.Current.CancellationToken);
        created.Id.ShouldBe(stored.Id);
    }

    // ---------------------------------------------------------------------------------------
    // UpdateSiteAsync
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task UpdateSiteAsync_WhenSiteDoesNotExist_ReturnsFalse()
    {
        var request = new SiteBuilder().BuildUpdateRequest();

        await using var actContext = _database.CreateContext();
        var updated = await new SiteService(actContext)
            .UpdateSiteAsync(404, request, TestContext.Current.CancellationToken);

        updated.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateSiteAsync_WhenSiteExists_ReturnsTrue()
    {
        var siteId = await SeedSiteAsync(new SiteBuilder());
        var request = new SiteBuilder().WithLocation("Relocated Shelf").BuildUpdateRequest();

        await using var actContext = _database.CreateContext();
        var updated = await new SiteService(actContext)
            .UpdateSiteAsync(siteId, request, TestContext.Current.CancellationToken);

        updated.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateSiteAsync_WhenSiteExists_PersistsTheChangedFields()
    {
        var siteId = await SeedSiteAsync(new SiteBuilder());

        var request = new SiteBuilder()
            .WithLocation("Southern Shelf, Sector 3")
            .WithCoordinates("12.0000° S, 45.0000° E")
            .WithPosition(-12.0, 45.0)
            .WithDescription("Re-surveyed after the 3rd expedition.")
            .WithPublicNarrative("Open to visitors from spring.")
            .WithAeonNarrative("Restricted: revised resonance profile.")
            .BuildUpdateRequest();

        await using var actContext = _database.CreateContext();
        await new SiteService(actContext)
            .UpdateSiteAsync(siteId, request, TestContext.Current.CancellationToken);

        await using var assertContext = _database.CreateContext();
        var stored = await assertContext.Sites.SingleAsync(s => s.Id == siteId, TestContext.Current.CancellationToken);
        stored.Location.ShouldBe("Southern Shelf, Sector 3");
        stored.Coordinates.ShouldBe("12.0000° S, 45.0000° E");
        stored.Latitude.ShouldBe(-12.0);
        stored.Longitude.ShouldBe(45.0);
        stored.Description.ShouldBe("Re-surveyed after the 3rd expedition.");
        stored.PublicNarrative.ShouldBe("Open to visitors from spring.");
        stored.AeonNarrative.ShouldBe("Restricted: revised resonance profile.");

        // Name is conspicuously missing from this list. That is not an oversight and it is not
        // correct behavior either: UpdateSiteAsync never assigns it, even though
        // UpdateSiteRequest.Name is [Required] - so a rename silently does nothing. The failing
        // test that pins that down, and the fix, are the next milestone. Covering the fields
        // that do work first keeps the red test about one thing.
    }

    // ---------------------------------------------------------------------------------------
    // DeleteSiteAsync
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task DeleteSiteAsync_WhenSiteExists_RemovesTheRowAndReturnsTrue()
    {
        var siteId = await SeedSiteAsync(new SiteBuilder());

        await using var actContext = _database.CreateContext();
        var deleted = await new SiteService(actContext)
            .DeleteSiteAsync(siteId, TestContext.Current.CancellationToken);

        deleted.ShouldBeTrue();

        await using var assertContext = _database.CreateContext();
        (await assertContext.Sites.AnyAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteSiteAsync_WhenSiteDoesNotExist_ReturnsFalse()
    {
        await using var actContext = _database.CreateContext();

        var deleted = await new SiteService(actContext)
            .DeleteSiteAsync(404, TestContext.Current.CancellationToken);

        deleted.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteSiteAsync_WhenOtherSitesExist_LeavesThemUntouched()
    {
        var doomedId = await SeedSiteAsync(new SiteBuilder().WithName("Ashfall Terrace"));
        await SeedSiteAsync(new SiteBuilder().WithName("Glasswater Basin"));

        await using var actContext = _database.CreateContext();
        await new SiteService(actContext).DeleteSiteAsync(doomedId, TestContext.Current.CancellationToken);

        // A delete that removes one row too many is the kind of bug a single-row test cannot
        // see, so there is always a bystander in the database.
        await using var assertContext = _database.CreateContext();
        var survivor = await assertContext.Sites.SingleAsync(TestContext.Current.CancellationToken);
        survivor.Name.ShouldBe("Glasswater Basin");
    }

    // ---------------------------------------------------------------------------------------
    // ArchiveSiteAsync
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task ArchiveSiteAsync_WhenSiteDoesNotExist_ReturnsFalse()
    {
        await using var actContext = _database.CreateContext();

        var archived = await new SiteService(actContext)
            .ArchiveSiteAsync(404, TestContext.Current.CancellationToken);

        archived.ShouldBeFalse();
    }

    [Fact]
    public async Task ArchiveSiteAsync_WhenSiteExists_AppendsTheArchivedSuffixAndReturnsTrue()
    {
        var siteId = await SeedSiteAsync(new SiteBuilder().WithName("Ashfall Terrace"));

        await using var actContext = _database.CreateContext();
        var archived = await new SiteService(actContext)
            .ArchiveSiteAsync(siteId, TestContext.Current.CancellationToken);

        archived.ShouldBeTrue();

        await using var assertContext = _database.CreateContext();
        var stored = await assertContext.Sites.SingleAsync(s => s.Id == siteId, TestContext.Current.CancellationToken);
        stored.Name.ShouldBe($"Ashfall Terrace{ArchivedSuffix}");

        // Archiving twice appends the suffix twice, which is a real bug - see the next
        // milestone. This test covers only the first call, which does behave.
    }
}
