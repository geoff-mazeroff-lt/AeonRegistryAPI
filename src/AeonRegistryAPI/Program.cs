// -- Builder section: set up services and configurations --------------
var builder = WebApplication.CreateBuilder(args);
builder.AddApplicationServices();

var app = builder.Build();

// -- App section: set up middleware pipeline and handle HTTP requests -------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Seeding needs a live Postgres: DataSeed.ManageDataAsync applies the Npgsql migrations and
// resets Postgres sequences with raw SQL, neither of which works against SQLite.
// Tests run in the "Testing" environment and skip it.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    await DataSeed.ManageDataAsync(scope.ServiceProvider);
}

app.UseApplicationPipeline();
app.MapApplicationEndpoints();

app.Run();

// Program is implicitly internal and sealed when using top-level statements. Declaring the
// partial type here makes it visible so tests can write WebApplicationFactory<Program>.
public partial class Program;
