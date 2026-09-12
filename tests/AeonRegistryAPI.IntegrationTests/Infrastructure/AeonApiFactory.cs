using System.Net.Http.Headers;
using System.Net.Http.Json;
using AeonRegistryAPI.Data;
using AeonRegistryAPI.Models;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AeonRegistryAPI.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real application in-process for levels 3 and 4, with Npgsql swapped for SQLite
/// in-memory and a small set of Identity users seeded.
/// </summary>
/// <remarks>
/// <para>
/// This is the whole app: the same <c>AddApplicationServices</c>, the same middleware pipeline,
/// the same endpoint mapping, reached over a real <see cref="HttpClient"/>. That is what makes
/// routing, model binding, <c>AddValidation()</c>, authentication, authorization, status codes
/// and JSON serialization observable - none of which the service-level tests can see. There is
/// no network socket involved; <c>WebApplicationFactory</c> wires the client straight to an
/// in-memory server.
/// </para>
/// <para>
/// <b>Cost:</b> building the host and hashing seeded passwords takes on the order of a second,
/// which is why this is shared per test class via <c>IClassFixture</c> while the service tests
/// build a fresh database each. Domain rows are cleared between tests with
/// <see cref="ResetDomainDataAsync"/> instead, leaving the expensive Identity rows in place.
/// </para>
/// <para>
/// <b>Two things about the app make this work without special-casing.</b> First,
/// <c>Program.cs</c> skips <c>DataSeed</c> in the <c>Testing</c> environment - it applies the
/// Npgsql migrations and runs raw Postgres SQL, so it cannot run here. Second,
/// <c>UseHttpsRedirection</c> is in the pipeline but goes quiet in a test host: with no
/// listening HTTPS port to redirect to, it logs a warning and passes the request through. If it
/// ever did redirect, every test would see a 307 instead of its expected status.
/// </para>
/// </remarks>
public sealed class AeonApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string Password = "Aeon-Test-Pass1!";

    public const string ArchivistRole = "Archivist";
    public const string PlainUserEmail = "researcher@aeon.test";
    public const string ArchivistUserEmail = "archivist@aeon.test";

    // Held open for the lifetime of the factory: an in-memory SQLite database exists only while
    // a connection to it does. Same reasoning as SqliteDatabaseFixture, one level up.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public AeonApiFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Skips DataSeed (see the class remarks). Any name other than Development also keeps
        // UseSwagger/UseSwaggerUI out of the pipeline - the contract test does not need them,
        // because AddCustomSwagger registers ISwaggerProvider unconditionally in the builder.
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Both registrations have to go, and this is the trap the plan warns about.
            // AddDbContext registers the options object *and*, since EF Core 9, an
            // IDbContextOptionsConfiguration<T> that holds the `options => options.UseNpgsql(...)`
            // callback. Remove only DbContextOptions<T> and the callback survives, re-applies
            // Npgsql over the new registration, and the tests fail trying to reach a Postgres
            // server that isn't there - with an error that says nothing about any of this.
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
        });
    }

    /// <summary>
    /// Creates the schema and seeds the Identity rows the auth tests log in as.
    /// </summary>
    /// <remarks>
    /// xUnit awaits this once for the class fixture, before the first test. Touching
    /// <c>Services</c> is what actually builds the host, so the cost lands here rather than
    /// inside whichever test happened to run first.
    /// </remarks>
    public async ValueTask InitializeAsync()
    {
        using var scope = Services.CreateScope();

        // EnsureCreated, not Migrate: the migrations are Npgsql-specific. CI checks the model
        // against the migration snapshot separately.
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await roleManager.CreateAsync(new IdentityRole(ArchivistRole));

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await CreateUserAsync(userManager, PlainUserEmail, role: null);
        await CreateUserAsync(userManager, ArchivistUserEmail, ArchivistRole);
    }

    private static async Task CreateUserAsync(UserManager<ApplicationUser> userManager, string email, string? role)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User"
        };

        // Not silent on failure: a mistyped password that violates the Identity rules would
        // otherwise show up much later as an unexplained 401 in an unrelated test.
        var result = await userManager.CreateAsync(user, Password);
        result.Succeeded.ShouldBeTrue(
            $"Seeding {email} failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        if (role is not null)
        {
            (await userManager.AddToRoleAsync(user, role)).Succeeded.ShouldBeTrue();
        }
    }

    /// <summary>
    /// Returns a client that logs in for real and carries the resulting bearer token.
    /// </summary>
    /// <param name="role">
    /// <c>null</c> for a user with no roles, or <see cref="ArchivistRole"/> for one that has it.
    /// </param>
    /// <remarks>
    /// <para>
    /// This goes through <c>POST /api/auth/login</c> and attaches the token the API actually
    /// issues, rather than injecting a fake <c>ClaimsPrincipal</c>. That matters for one test in
    /// particular: the 403 on the Archivist-only archive endpoint is a test <i>of the role
    /// plumbing</i> - the claim landing in the token, the token being read back, the policy
    /// matching the role. Stub the identity and that test asserts nothing but the stub.
    /// </para>
    /// <para>
    /// The alternative is worth knowing for suites where auth is not the subject: register a
    /// test <c>AuthenticationHandler</c> in <c>ConfigureTestServices</c> that unconditionally
    /// succeeds with whatever claims the test asks for. It is much faster - no password hashing,
    /// no login round-trip - and it is the right choice once you have a hundred authenticated
    /// tests and one place that proves the real thing works.
    /// </para>
    /// </remarks>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string? role = null)
    {
        var email = role == ArchivistRole ? ArchivistUserEmail : PlainUserEmail;

        var client = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = Password },
            TestContext.Current.CancellationToken);

        // Assert on the login itself so a broken login fails here, naming the reason, instead of
        // as a puzzling 401 inside the test that wanted the client.
        response.IsSuccessStatusCode.ShouldBeTrue(
            $"Login for {email} returned {(int)response.StatusCode}.");

        var token = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(
            TestContext.Current.CancellationToken);
        token.ShouldNotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        return client;
    }

    /// <summary>A context over this factory's database, for arranging and asserting rows directly.</summary>
    public ApplicationDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options);

    /// <summary>
    /// Deletes all domain rows, leaving the seeded Identity rows alone.
    /// </summary>
    /// <remarks>
    /// Called from each test class's <c>InitializeAsync</c>, so tests still start from an empty
    /// registry without paying to re-seed users. Deletion runs child-to-parent -
    /// notes, records, media, artifacts, then sites - because the foreign keys are enforced.
    /// </remarks>
    public async Task ResetDomainDataAsync()
    {
        await using var context = CreateDbContext();

        await context.CatalogNotes.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.CatalogRecords.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.ArtifactMediaFiles.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.Artifacts.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.Sites.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
