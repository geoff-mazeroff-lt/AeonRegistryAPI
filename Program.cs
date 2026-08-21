using AeonRegistryAPI.Endpoints.Artifact;
using AeonRegistryAPI.Endpoints.CustomIdentity;
using AeonRegistryAPI.Endpoints.Home;
using AeonRegistryAPI.Endpoints.Site;
using AeonRegistryAPI.Middleware;
using AeonRegistryAPI.Models;
using AeonRegistryAPI.Services;
using AeonRegistryAPI.Services.Artifact;
using AeonRegistryAPI.Services.ArtifactMedia;
using AeonRegistryAPI.Services.Site;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

// -- Builder section: set up services and configurations --------------
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomSwagger();

// Configure EF for Postgres
var connectionString = DataUtility.GetConnectionString(builder.Configuration);
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

// ASP.NET identity
// Assuming this is an internal app that doesn't require user to confirm their email.
builder.Services.AddIdentityApiEndpoints<ApplicationUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Admin policy
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

// Configure email
builder.Services.AddTransient<IEmailSender, ConsoleEmailService>();

// Enable validation for incoming DTOs
builder.Services.AddValidation();

// Custom services
builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<IArtifactMediaService, ArtifactMediaService>();
builder.Services.AddScoped<IArtifactService, ArtifactService>();

var app = builder.Build();

// -- App section: set up middleware pipeline and handle HTTP requests -------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    await DataSeed.ManageDataAsync(scope.ServiceProvider);
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // needed for images
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<BlockIdentityEndpoints>();

// Map API endpoints for login, logout, etc. using ASP.NET identity
var authRouteGroup = app.MapGroup("/api/auth")
    .WithTags("Admin");
authRouteGroup.MapIdentityApi<ApplicationUser>();

// Map custom endpoints
app.MapHomeEndpoints();
app.MapCustomIdentityEndpoints();
app.MapSiteEndpoints();
app.MapArtifactMediaFileEndpoints();
app.MapArtifactEndpoints();

app.Run();