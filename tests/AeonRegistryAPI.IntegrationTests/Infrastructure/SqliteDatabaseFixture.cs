using AeonRegistryAPI.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AeonRegistryAPI.IntegrationTests.Infrastructure;

/// <summary>
/// A throwaway SQLite database, created empty, for one test to use and then discard.
/// </summary>
/// <remarks>
/// <para>
/// This class exists because of a design fact about the app: <c>SiteService</c> takes the
/// concrete <see cref="ApplicationDbContext"/>, not an interface. There is no seam to
/// substitute, so a "unit" test of the service is not available - the only way to run its
/// code is to give it a real EF provider. That makes service tests level 2, not level 1.
/// In a codebase with a repository interface, most of what
/// <c>Services/SiteServiceTests</c> covers would collapse into fast unit tests instead.
/// </para>
/// <para>
/// <b>Why not Postgres?</b> The team has no Docker, so Testcontainers is out. SQLite runs
/// in-process with no setup at all, and the cost is real and worth stating plainly: these
/// tests cannot catch anything Npgsql-specific. Notably SQLite <i>ignores</i>
/// <c>MaxLength</c>, so a value too long for a column inserts happily here and throws in
/// production. Where a length limit matters, assert on the string, not on the database.
/// </para>
/// <para>
/// <b>Why not the EF in-memory provider?</b> It is not a relational provider: it does not
/// translate SQL, ignores most constraints, and will happily execute LINQ that Npgsql
/// rejects - which defeats the main purpose of these tests. SQLite is a real relational
/// provider and catches untranslatable queries.
/// </para>
/// </remarks>
public sealed class SqliteDatabaseFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteDatabaseFixture()
    {
        // An in-memory SQLite database lives exactly as long as a connection to it is open;
        // when the last one closes, the database is gone. So the fixture opens one connection
        // and holds it for its whole lifetime, and every DbContext it hands out is pointed at
        // that same connection - which is what makes them all see the same database.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();

        // EnsureCreated, not Migrate: the Migrations/ folder is generated for Npgsql and its
        // SQL will not apply to SQLite. The trade-off is that these tests never exercise the
        // migrations, so a migration that has drifted from the model still passes here. CI
        // closes that gap separately with
        // `dotnet ef migrations has-pending-model-changes` (see .github/workflows/ci.yml),
        // which needs no database at all.
        context.Database.EnsureCreated();
    }

    /// <summary>
    /// Creates a new <see cref="ApplicationDbContext"/> over this fixture's database.
    /// </summary>
    /// <remarks>
    /// Call this more than once per test. Arranging and asserting through the <i>same</i>
    /// context is the single most common way to write a DB test that proves nothing: the
    /// change tracker answers the assertion from memory, so the test passes even when
    /// nothing was ever written. A second context has an empty tracker and has to read the
    /// database.
    /// </remarks>
    public ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options);

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
}
