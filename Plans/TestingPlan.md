# Automated Testing for AeonRegistryAPI

## Context

This project was built by following a .NET 10 Minimal API course that never covered automated testing. There are no test projects, no test packages, and no CI. The goal is not just coverage — it's to build a **worked reference and teaching artifact** that shows a team at work what unit, integration, API, and contract testing look like in a real ASP.NET Core Minimal API codebase.

Constraints that shape everything below:

- **No Docker** on the target team's machines → Testcontainers is off the table. DB-backed tests run on **SQLite in-memory**.
- **No API consumer exists** → "contract testing" means **OpenAPI snapshot verification**, not Pact.
- Preferred stack: **xUnit, NSubstitute, Shouldly**.
- Scope: **Sites covered deeply across all four levels**, plus one representative example of each other technique. Artifacts / media / identity are left as exercises the team extends using the same patterns.
- Where tests reveal real bugs, write the test **red first, then fix the production code**.
- **Repo layout is in scope.** The web project currently *is* the repo root; it moves into `src/AeonRegistryAPI/` before any test project is added, so `src/` and `tests/` sit side by side in the conventional .NET shape (see deliverable 1).

### The structural fact that drives the design

`SiteService`, `ArtifactService`, and `ArtifactMediaService` inject the **concrete `ApplicationDbContext`** — there is no repository interface, no `IApplicationDbContext`. That means there is no seam to substitute, so *service tests are necessarily database tests*. This is worth teaching explicitly rather than papering over: it's why NSubstitute barely appears in the service layer, and it's the clearest illustration of how design choices determine which test levels are even available to you.

---

## The four levels, defined for this codebase

| Level | What runs | What it catches | What it can't |
|---|---|---|---|
| **Unit** | A single class, no DB, no host. NSubstitute for collaborators. | Branch logic in `ImageValidationHelper`, `BlockIdentityEndpoints`, `ExceptionHandlingFilter`. | Anything about EF translation, routing, or serialization. |
| **Integration (service + EF)** | Real `SiteService` against a real EF provider (SQLite in-memory). | LINQ that doesn't translate, projection mistakes, tracking/`SaveChanges` behavior, cascade deletes, `HasConversion<string>()`. | Postgres-specific SQL; routing, binding, auth, JSON. |
| **API (host-level)** | The whole app via `WebApplicationFactory` over `HttpClient`. | Routes, model binding, `AddValidation()` 400s, auth 401/403, status codes, JSON shape, endpoint filters. | Nothing about Postgres; timing/concurrency. |
| **Contract (OpenAPI)** | Generate the Swagger doc from the running host and diff it against a committed snapshot. | *Unintended* breaking changes to the published surface — a renamed route, a dropped property, a changed status code. | Whether the API is correct — only whether it changed. |

Two points to make in the README when teaching this: the levels are a **pyramid of confidence vs. cost**, and level 2 exists here *only because* there's no repository seam. In a codebase with one, most of level 2 would collapse into level 1.

---

## Deliverables

### 1. Solution layout

Today the web project *is* the repo root: `AeonRegistryAPI.csproj`, `Program.cs`, `wwwroot/`, `Migrations/` and the rest sit next to `README.md` and `.gitignore`. That works while there is exactly one project, but it stops working the moment a second one appears — there is no place to put `tests/` that doesn't make the repo root a mix of "the app" and "things about the app." So the first deliverable is the move: **`src/AeonRegistryAPI/` first, `tests/` second.** Doing it in that order means every `ProjectReference`, `.slnx` path, and CI path gets written once, correctly.

Target layout:

```
AeonRegistryAPI.slnx
Directory.Packages.props            (new — central package management, applies to src/ and tests/)
global.json                         (new — selects the Microsoft.Testing.Platform test runner)
README.md
.gitignore
Plans/
src/
  AeonRegistryAPI/
    AeonRegistryAPI.csproj
    Program.cs
    GlobalUsings.cs
    appsettings.json
    appsettings.Development.json
    AeonRegistryAPI.http
    Data/  Endpoints/  Enums/  Extensions/  Filters/  Helpers/
    Middleware/  Migrations/  Models/  Properties/  Services/  wwwroot/
tests/
  AeonRegistryAPI.UnitTests/
  AeonRegistryAPI.IntegrationTests/   (holds integration + API + contract tests)
.github/workflows/ci.yml             (new)
```

**Move mechanics.** Use `git mv` for every tracked item, not a filesystem copy — `git mv` keeps `git log --follow` working, which matters for a repo whose whole purpose is to be read. Delete the stale root `bin/` and `obj/` afterwards; they are gitignored but will confuse the first build. Note that the working tree already contains an **empty `src/AeonRegistryAPI/` directory skeleton** from an earlier attempt (directories only, no files, untracked) — remove it before starting so the move lands cleanly.

What stays at the repo root: `README.md`, `.gitignore`, `Plans/`, `AeonRegistryAPI.slnx`, the new `Directory.Packages.props`, `.github/`, and the gitignored Rider/user files. The root `secrets.json` is gitignored and superseded by the `UserSecretsId` store under `%APPDATA%` (see README) — leave it alone rather than growing the diff.

**What the move breaks, and the fix for each:**

| Reference | Fix |
|---|---|
| `AeonRegistryAPI.slnx` project path | `src/AeonRegistryAPI/AeonRegistryAPI.csproj` |
| `dotnet run` / `dotnet ef` from the repo root | `--project src/AeonRegistryAPI`, or `cd` into it. Both the README "Entity Framework reminders" section and CI step 3 need this. |
| README `Data/SeedData/`, the whole **Project structure and conventions** section, and "Running the project" | Re-root the paths under `src/AeonRegistryAPI/` and show the `--project` form of `dotnet run` |
| `DataSeed.cs:41` — `Path.Combine(Directory.GetCurrentDirectory(), "Data", "SeedData")` | Behavior is unchanged (`dotnet run` sets the CWD to the project directory), but the move makes the latent bug easy to hit: run the published DLL, or `dotnet test` from the root, and the path resolves to nothing. Switch it to `IWebHostEnvironment.ContentRootPath` while doing the section-3 `DataSeed` edit. |

Nothing in `wwwroot/` needs touching: it moves with the project, so `app.UseStaticFiles()` still serves `/site/sites-map.html` from the same URL.

Then the two test projects. Two rather than one, or four: the unit project must stay **fast and dependency-free** so it can run on every save; the other three levels all share the same SQLite + `WebApplicationFactory` infrastructure and splitting them would duplicate it for no benefit. Folders inside the integration project (`Services/`, `Api/`, `Contract/`) keep the levels visually distinct.

`AeonRegistryAPI.slnx` becomes:

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/AeonRegistryAPI/AeonRegistryAPI.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/AeonRegistryAPI.UnitTests/AeonRegistryAPI.UnitTests.csproj" />
    <Project Path="tests/AeonRegistryAPI.IntegrationTests/AeonRegistryAPI.IntegrationTests.csproj" />
  </Folder>
</Solution>
```

Both test projects reference the app as `<ProjectReference Include="../../src/AeonRegistryAPI/AeonRegistryAPI.csproj" />`.

### 2. Packages (versions verified against nuget.org)

**xUnit v3** (`xunit.v3` 4.0.0), not v2 — it's what `dotnet new xunit` produces on the .NET 10 SDK, and it runs on Microsoft.Testing.Platform. Note for the team: v3 test projects are `OutputType=Exe`, and `IAsyncLifetime` returns `ValueTask`, not `Task`.

`Directory.Packages.props` (central version management, so the team sees one place to bump):

| Package | Version | Where |
|---|---|---|
| `xunit.v3` | 4.0.0 | both test projects |
| `Microsoft.Testing.Extensions.CodeCoverage` | 18.11.0 | both |
| `NSubstitute` | 6.2.0 | both |
| `Shouldly` | 4.3.0 | both |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.11 | integration only |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.11 | integration only |
| `Verify.XunitV3` | 31.28.0 | integration only (contract snapshot) |

**Revised during milestone 2** from the original list, which named `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`, and `coverlet.collector`. All three are VSTest-era packages and none of them belongs here: xUnit v3 compiles its own runner into the test assembly and executes on Microsoft.Testing.Platform, and `Microsoft.Testing.Extensions.CodeCoverage` is the MTP equivalent of coverlet (see the `--coverage` switch in section 7). Adding them back is harmless but misleading — the team should not go looking for a VSTest pipeline that isn't there.

Existing app packages move into `Directory.Packages.props` too, and bump EF/ASP.NET to a consistent 10.0.11 to match the test-side Sqlite provider (mismatched EF provider versions are a common and confusing failure).

### 3. Production changes for testability

All paths below are relative to `src/AeonRegistryAPI/` after the section-1 move.

**`Program.cs` → composable extensions**, so production and tests assemble the host the same way. Three new/edited files in `Extensions/` (matching the existing `OpenAPISwaggerExtensions.cs` convention):

- `Extensions/ServiceCollectionExtensions.cs` — `AddApplicationServices(this WebApplicationBuilder builder)`: Swagger, `AddDbContext`, Identity, the `AdminOnly` policy, `IEmailSender`, `AddValidation()`, and the three scoped services.
- `Extensions/WebApplicationExtensions.cs` — `UseApplicationPipeline(this WebApplication app)` (HTTPS redirect, static files, authn/authz, `BlockIdentityEndpoints`) and `MapApplicationEndpoints(this WebApplication app)` (the identity route group + the five `Map*Endpoints()` calls).
- `Program.cs` reduces to roughly:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddApplicationServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Seeding needs a live Postgres and reads Data/SeedData off the current directory,
// so tests run in the "Testing" environment and skip it.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    await DataSeed.ManageDataAsync(scope.ServiceProvider);
}

app.UseApplicationPipeline();
app.MapApplicationEndpoints();

app.Run();

// Exposes the implicit Program type so WebApplicationFactory<Program> can reference it.
public partial class Program;
```

Skipping seeding is not optional: `DataSeed.ManageDataAsync` calls `Database.MigrateAsync()` (Npgsql migrations, which will not apply to SQLite) and `ResetPostgresSequencesAsync` (raw Postgres SQL).

While in there, two fixes to `Data/DataSeed.cs`: the pre-existing `await using var dbContextSvc = svcProvider.GetRequiredService<ApplicationDbContext>()` at `ManageDataAsync:19` disposes a scoped service it doesn't own, and the `Directory.GetCurrentDirectory()` seed path at line 41 should become `IWebHostEnvironment.ContentRootPath` (see the section-1 table).

### 4. Shared test infrastructure

**`SqliteDatabaseFixture`** (integration project, `Infrastructure/`). The two things that trip people up — an in-memory SQLite database only lives as long as its connection is open, and Npgsql migrations can't be applied to SQLite — are exactly what this class exists to demonstrate, so it gets a comment block explaining both:

```csharp
public sealed class SqliteDatabaseFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteDatabaseFixture()
    {
        // An in-memory SQLite database is destroyed the moment its last connection closes.
        // We hold one open for the lifetime of the fixture so every DbContext we hand out
        // sees the same database.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();

        // EnsureCreated, not Migrate: the Migrations/ folder is Npgsql-specific.
        // CI verifies model/migration drift separately (see ci.yml).
        context.Database.EnsureCreated();
    }

    public ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options);

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
}
```

**Isolation strategy: a fresh fixture per test** (`IAsyncLifetime` on the test class, not `IClassFixture`). An in-memory SQLite database costs roughly a millisecond to create, and per-test isolation removes an entire category of order-dependent flakiness that would otherwise be the first thing the team hits. Transaction-rollback isolation is faster but interferes with code under test that manages its own transactions — mention it as the alternative, don't use it.

**Always use a separate `DbContext` for arrange and for assert.** A single context would satisfy assertions from its change tracker without ever touching the database. This is the most valuable single habit to teach in this file.

**`AeonApiFactory : WebApplicationFactory<Program>`** for API and contract tests:

- `UseEnvironment("Testing")` → skips seeding.
- In `ConfigureTestServices`, remove **both** `DbContextOptions<ApplicationDbContext>` *and* `IDbContextOptionsConfiguration<ApplicationDbContext>` before re-registering SQLite. EF Core 9+ registers the latter, and removing only the former leaves Npgsql configured — this is a known, confusing failure and deserves a comment. Note the namespace: `IDbContextOptionsConfiguration<T>` lives in **`Microsoft.EntityFrameworkCore.Infrastructure`**, not the root `Microsoft.EntityFrameworkCore`, so the obvious `using` does not resolve it (confirmed by reflection over the EF 10.0.11 assembly during milestone 8).
- `ConfigureTestServices` itself is an extension on `IWebHostBuilder` from **`Microsoft.AspNetCore.TestHost`**, and `AccessTokenResponse` — the login response type worth deserializing into rather than hand-rolling — is in **`Microsoft.AspNetCore.Authentication.BearerToken`**. Both are easy to hunt for.
- `EnsureCreated()` on the shared connection, then seed a minimal Identity set: an `Archivist` role, one plain user, one Archivist user.
- Expose `ResetDomainDataAsync()` that clears `Sites` / `Artifacts` / `ArtifactMediaFiles` between tests while leaving the Identity rows intact.

**Authentication in API tests: log in for real.** Seed users via `UserManager<ApplicationUser>`, then `POST /api/auth/login` and attach the returned bearer token. A stub `AuthenticationHandler` would be faster, but the 403 test for the `Archivist`-only archive endpoint is precisely a test *of the role plumbing* — stubbing the identity would make it assert nothing. Provide `factory.CreateAuthenticatedClientAsync(role: null | "Archivist")` so tests stay one line. Note the stub-handler alternative in a comment for teams whose auth isn't the thing under test.

**Test data builders: hand-rolled, not Bogus.** A `SiteBuilder` in `TestData/` with valid defaults and fluent `WithName(...)` / `WithoutLocation()` overrides. Deterministic, no extra dependency, and it makes each test's *relevant* input obvious against a background of defaults — which is the actual teaching point. Bogus is worth a one-line mention for bulk data generation.

### 5. Test classes to write

**Unit** (`AeonRegistryAPI.UnitTests`) — this is where NSubstitute earns its place:

- `Helpers/ImageValidationHelperTests` — the richest pure-logic target. `Substitute.For<IFormFile>()` with `Length`, `ContentType`, `FileName`, and `OpenReadStream()` configured. Theory-drive the content-type and extension tables; separate facts for empty, oversized, good magic bytes per format, and bad magic bytes. Shows `Should.ThrowAsync<InvalidOperationException>()`.
    - Note: the `header[0..3]` indexing is **not** an out-of-bounds risk — `header` is always an 8-byte zero-filled array. There *is* a real gap: `file.ContentType` is dereferenced without a null check. Cover it.
- `Middleware/BlockIdentityEndpointsTests` — `DefaultHttpContext` + a `RequestDelegate` spy. Asserts 404 for a blocked path and pass-through otherwise. **Write these red**: the middleware uses exact match, so `/API/Auth/Register` and `/api/auth/register/` currently slip through. Also worth a test documenting that the Swagger hide-list in `OpenAPISwaggerExtensions.cs` and this block-list disagree.
- `Filters/ExceptionHandlingFilterTests` — verifies a thrown exception becomes a 500 `ProblemDetails` and that `detail` is populated only in Development. Uses NSubstitute for `IWebHostEnvironment` and an `EndpointFilterDelegate` that throws.

Endpoint handlers are **not** unit tested: they're `private static` methods, and making them visible purely for testing would be a worse trade than covering them at the API level, where routing and binding get exercised too. Say so in the README — "why we didn't test this" is as instructive as what we did.

**Integration** (`Services/SiteServiceTests`) — the deep slice. All eight `ISiteService` methods, arrange/assert through separate contexts:

- `GetAllPublicSitesAsync` returns every site, and returns an empty collection rather than `null` when there are none. The **`AeonNarrative` omission is asserted at the API level, not here**: `PublicSiteResponse` has no such property, so at the service level there is nothing to assert that the compiler doesn't already enforce — and deserializing a response into that DTO would silently discard a leaked key. The only assertion that can fail is the one against the raw JSON body (see the API list below).
- `GetPublicSiteByIdAsync` / `GetPrivateSiteByIdAsync` return `null` for a missing id.
- `CreateSiteAsync` persists and returns the DB-assigned `Id`.
- `UpdateSiteAsync` returns `false` for a missing id; updates every field for a real one. **Red:** it currently never assigns `Name`, even though `UpdateSiteRequest.Name` is `[Required]`.
- `DeleteSiteAsync` removes the row; returns `false` when absent.
- `ArchiveSiteAsync` appends the suffix. **Red:** calling it twice appends twice, and enough calls overflow the 200-char `MaxLength` on `Site.Name`.

**API** (`Api/SiteEndpointsTests`) — over `HttpClient`:

- `GET /api/public/sites` → 200 unauthenticated, and the JSON body has no `aeonNarrative` key. Assert against a `JsonNode`, never a deserialized `PublicSiteResponse` — that DTO would drop a leaked key and the test would pass exactly when it should fail.
- `GET /api/public/sites/{id}` → 200 for a real id, 404 for a missing one, and 404 for a non-numeric one (the `{id:int}` constraint answering from routing rather than a 400 from binding).
- `GET /api/private/sites` unauthenticated → 401; authenticated → 200 *with* `aeonNarrative`.
- `POST /api/private/sites` with a missing `Name` → 400 `ValidationProblemDetails` (proves `AddValidation()` is actually wired). Send an anonymous object, not a `CreateSiteRequest` — it is the only way to omit the property outright.
- `POST /api/private/sites` valid → 201 with a `Location` header pointing at the created resource; **follow the header** and assert the GET succeeds, because a hand-built URL string is easy to get subtly wrong and impossible to spot by reading.
- `POST /api/private/sites` unauthenticated → 401, *and nothing was persisted*.
- `POST /api/private/sites/{id}/archive/` as a non-Archivist → 403 with the name unchanged; as an Archivist → 204 with the suffix applied; as an Archivist against a missing id → 404. *(the auth example)*
- **Red:** `PUT /api/private/sites/{id}` and `DELETE /api/private/sites/{id}` currently 404, because the routes are `MapPut("")` / `MapDelete("")` and `id` binds from the query string instead. Each fix gets a *pair* of tests — the RESTful URL now works, and the old query-string URL no longer does. The second half matters: leaving both live would be two URLs for one resource, which survives indefinitely because both "work". Once `{id:int}` is in place the retired form returns **405**, not 404, because `/api/private/sites` is still a real route for `GET` and `POST` — the matcher finds the pattern and rejects only the verb.
- The same two route bugs on Artifacts (bug 5) get a thin version of the same treatment in `Api/ArtifactEndpointsTests` and `Api/ArtifactMediaFileEndpointsTests`, even though Artifacts are otherwise out of the deep slice. `POST /api/private/artifacts/media-file/{artifactId}` is the one worth reading: it is the only multipart endpoint, and it is where the route-vs-query distinction is genuinely easy to miss, since minimal APIs bind simple types beside an `IFormFile` from the route or query and never from the form. Its retired URL returns 404 rather than 405, because that group had only the one endpoint and moving it left the bare path with no routes at all.

**Contract** (`Contract/OpenApiContractTests`) — one test:

```csharp
var swagger = factory.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
await Verify(swagger.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0));
```

No production change needed: `AddCustomSwagger()` is registered unconditionally in the builder — only `UseSwagger()` is Development-gated — so `ISwaggerProvider` is resolvable from the test host. Verify writes `*.received.json` next to `*.verified.json` on mismatch, which gives the team a readable diff and a one-command accept. Commit the `.verified.json` and add `*.received.*` to `.gitignore`. Frame it plainly: this test failing means "you changed the public contract" — it is a prompt to think, not necessarily a bug.

### 6. Bug-fix sequence

Each is: failing test → production fix → confirm green.

| # | Bug | Fix | Status |
|---|---|---|---|
| 1 | `PUT /api/private/sites` has no `{id:int}`; `id` binds from query string | `MapPut("{id:int}")` — **breaking API change**. Verified no callers in `src/AeonRegistryAPI/wwwroot/site/sites-map.js` (public GETs only); there is no `.http` file in the repo. | **fixed (milestone 9)** |
| 2 | Same on `MapDelete("")` for sites | `MapDelete("{id:int}")` | **fixed (milestone 9)** |
| 3 | `SiteService.UpdateSiteAsync` never assigns `Name` | Add `existingSite.Name = request.Name;` | **fixed (milestone 7)** |
| 4 | `ArchiveSiteAsync` is not idempotent; can overflow `MaxLength(200)` | Return early (still `true`) if the name already ends in the suffix | **fixed (milestone 7)** — suffix extracted to a `const`, ordinal `EndsWith`, null-guarded. Returns `true` rather than `false` because `false` is the service's "not found" signal and the endpoint maps it to 404. Known limitation: a site a human names `"Foo [ARCHIVED]"` will no-op on archive; the design without that hole is a separate `IsArchived` column, which is a schema change and out of scope here. |
| 5 | Same query-string route bug on `ArtifactEndpoints.MapPut("")` and `ArtifactMediaFileEndpoints.MapPost("")` | Route-only change; cheap and keeps conventions consistent even though Artifacts are outside the deep slice. `MapPut("{id:int}")`, matching the `MapDelete` beside it that was already correct, and `MapPost("{artifactId:int}")` — `isPrimary` stays a query parameter, since it is a flag about the upload rather than part of the resource identity. | **fixed (milestone 9)** |

Two documentation fixes on `SiteEndpoints` belong with milestone 9, since they change the published surface and must land before the snapshot baseline in milestone 10: the archive endpoint was missing `.Produces(StatusCodes.Status403Forbidden)` despite being the one endpoint in the app with a role requirement, and its `.WithDescription()` did not state idempotency, which the README's own Description convention calls for. Both are done.

Deliberately deferred (flagged, not fixed): `ArtifactService.CreateArtifactAsync` throws `ArgumentException` for an unparseable `Type`, surfacing as a 500 instead of a 400. Fixing it properly means giving `IArtifactService` a result type that distinguishes "site not found" from "invalid type" — a design change that belongs with the Artifacts slice, not the Sites one. Same for `ArtifactMediaService.GetPublicArtifactImageByIdAsync` filtering on `IsPrimary`, which 404s non-primary images whose ids the API itself hands out.

### 7. CI — `.github/workflows/ci.yml`

`ubuntu-latest`, `actions/setup-dotnet@v4` with `10.0.x`, no services block and no Docker:

1. `dotnet restore` / `dotnet build --no-restore -c Release`
2. ✅ `dotnet test --no-build -c Release --coverage` — the Microsoft.Testing.Platform switch, backed by `Microsoft.Testing.Extensions.CodeCoverage`. **Not** the VSTest `--collect:"XPlat Code Coverage"` this step originally named; there is no VSTest in this solution (see section 2).
3. ✅ `dotnet ef migrations has-pending-model-changes --project src/AeonRegistryAPI` — the mitigation for using `EnsureCreated` instead of migrations in tests. It compares the model to the snapshot and needs no database, so it catches the drift SQLite tests structurally cannot.
4. ✅ `git diff --exit-code` after the test step, so an accepted-but-uncommitted OpenAPI snapshot fails the build.

### 8. README

Two edits. First, the mechanical one from milestone 1: re-root every path in **Project structure and conventions** under `src/AeonRegistryAPI/`, add `src/` and `tests/` as top-level entries so the layout itself is documented, and update "Running the project", "Seed data", and "Entity Framework reminders" for the `--project src/AeonRegistryAPI` form.

Second, add a **Testing** section following the file's existing conventions style: the four-level table above, the naming convention (`Method_Scenario_ExpectedResult`), how to run each level, why service tests need a database here, why endpoint handlers aren't unit tested, and how to accept an OpenAPI snapshot change. This is what makes the repo usable as a teaching artifact rather than just a tested repo.

**Already landed, out of milestone order** (done alongside milestone 8, because a reader who clones the repo today cannot run `dotnet test` without it): a **Running the tests** section covering `dotnet test`, the single-project fast loop, `--coverage`, the "no database, no Docker" point, and the `global.json` runner opt-in with the misleading VSTest error text. The `tests/` entry under **Project structure and conventions** now also describes both projects, their folders, and the test naming convention. What milestone 11 still owes: the four-level table, why service tests need a database in this codebase, why endpoint handlers aren't unit tested, the arrange/assert-with-separate-contexts habit, the two isolation strategies and why levels 2 and 3 chose differently (fresh `SqliteDatabaseFixture` per test vs. a shared `AeonApiFactory` with `ResetDomainDataAsync`, because seeded password hashing is what costs), the log-in-for-real auth helper and its stub-handler alternative, and the OpenAPI snapshot accept workflow.

The run instructions must cover the **test-runner opt-in**, which was discovered during milestone 2 and is not obvious from anything else in the repo. xUnit v3 runs on Microsoft.Testing.Platform, and the .NET 10 SDK removed the VSTest entry point, so `dotnet test` works only because the root `global.json` contains `"test": { "runner": "Microsoft.Testing.Platform" }`. Without it every test project fails with *"Testing with VSTest target is no longer supported"* — an error that names neither `global.json` nor the runner. Neither `dotnet.config` nor `-p:TestingPlatformDotnetTestSupport=true` is an accepted substitute on this SDK. The same paragraph is the right place to document coverage, since the MTP switch is `dotnet test --coverage` (backed by `Microsoft.Testing.Extensions.CodeCoverage`) rather than the VSTest `--collect:"XPlat Code Coverage"` that section 7 still names.

---

## Milestones

Each is independently verifiable — stop and run the suite after every one.

**Status: milestones 1–9 are complete.** The suite is green at 116 tests: 70 in `AeonRegistryAPI.UnitTests` and 46 in `AeonRegistryAPI.IntegrationTests` (20 service-level, 26 API-level). Counts include theory cases, which is why the unit number is larger than the number of test methods. Next up is milestone 10 — and its snapshot now baselines the corrected route surface, which was the whole reason milestone 9 came first. Sections above have been revised in place where the implementation taught us something the plan had wrong — see section 2 (packages), section 4 (namespaces), section 5 (where the `AeonNarrative` assertion belongs), section 6 (bug status), section 7 (coverage switch), and section 8 (what the README already covers).

1. ✅ **Move the web project to `src/AeonRegistryAPI/`** — `git mv` the tracked files, update the `.slnx` path, re-root the README paths and `dotnet run` / `dotnet ef` invocations. No test projects yet, so this milestone stands alone and is easy to review. Verify: `dotnet build`, `dotnet run --project src/AeonRegistryAPI` boots, Swagger renders, `/site/sites-map.html` still loads, `dotnet ef migrations list --project src/AeonRegistryAPI` works, and `git log --follow src/AeonRegistryAPI/Program.cs` shows the pre-move history.
2. ✅ `Directory.Packages.props`, both test projects, `.slnx` update. Verify: `dotnet test` runs and reports zero tests.
3. ✅ `Program.cs` refactor into extensions + `public partial class Program`. Verify: `dotnet run` still boots and Swagger still renders.
4. ✅ Unit tests — `ImageValidationHelper`, `ExceptionHandlingFilter`. Verify: green.
5. ✅ `BlockIdentityEndpoints` unit tests, red first, then fix casing/trailing-slash matching.
6. ✅ `SqliteDatabaseFixture` + `SiteBuilder`, and `SiteServiceTests` for the already-correct methods. Verify: green.
7. ✅ Red tests for bugs 3 and 4, then fix `SiteService`. Verify: green.
8. ✅ `AeonApiFactory` + auth helper + `SiteEndpointsTests` for existing-correct behavior. Verify: green.
9. ✅ Red tests for bugs 1, 2, 5, then fix the routes. Verify: green.
10. `OpenApiContractTests` + committed snapshot (generated *after* the route fixes so the baseline is the corrected surface).
11. CI workflow + README Testing section. Include the `global.json` test-runner opt-in and the MTP coverage switch (see section 8). Verify: workflow green on a pushed branch.

## Risks and gotchas to call out in code comments

- **SQLite ≠ Postgres.** Knowingly accepted. It won't catch Npgsql-specific SQL, identity-sequence behavior, case-sensitive collation, or `MaxLength` truncation (SQLite ignores length constraints — which is *why* bug 4's overflow is asserted at the service level by measuring the string, not by expecting a DB error). Document this limitation prominently; it's the single most important caveat for the team.
- **`EnsureCreated` bypasses migrations**, so a broken migration passes CI. Mitigated by step 3 of the workflow, not eliminated.
- **`IDbContextOptionsConfiguration<T>`** must be removed alongside `DbContextOptions<T>` in `ConfigureTestServices`, or SQLite silently doesn't take effect.
- **EF provider version skew** between `Npgsql.EntityFrameworkCore.PostgreSQL` and `Microsoft.EntityFrameworkCore.Sqlite` produces obscure runtime errors — central package management exists partly to prevent this.
- **Identity on SQLite** works, but password hashing makes seeded logins noticeably slow; seed users once per factory, not per test.
- **`.slnx` tooling** is still newer than `.sln`; if an IDE misbehaves, `dotnet test` at the solution level is the source of truth. Expect Rider to need a solution reload after the `src/` move, and `.idea/` / `*.DotSettings.user` to hold stale paths — both are gitignored, so deleting them is safe.
- **Root-relative CLI habits break after the move.** `dotnet run`, `dotnet ef`, and `dotnet user-secrets` all need `--project src/AeonRegistryAPI` when invoked from the repo root. This is the most common post-move friction; the README update in section 8 is the mitigation, not an afterthought.
- **`DateTime.Now` in `HomeEndpoints`** makes that response unsnapshotable. Not in scope to fix, but worth naming as the canonical example of why `TimeProvider` exists.

## Verification

```powershell
dotnet build
dotnet test                                  # all three projects
dotnet test tests/AeonRegistryAPI.UnitTests  # fast loop, no DB
dotnet ef migrations has-pending-model-changes --project src/AeonRegistryAPI
dotnet run --project src/AeonRegistryAPI     # app still boots against real Postgres
git log --follow src/AeonRegistryAPI/Program.cs   # move preserved history
```

Then confirm by hand: `https://localhost:7132/swagger` still renders, and `/site/sites-map.html` still loads sites (it only calls public GET routes, which are unchanged).
