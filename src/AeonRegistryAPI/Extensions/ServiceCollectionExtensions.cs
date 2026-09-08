using AeonRegistryAPI.Models;
using AeonRegistryAPI.Services;
using AeonRegistryAPI.Services.Artifact;
using AeonRegistryAPI.Services.ArtifactMedia;
using AeonRegistryAPI.Services.Site;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace AeonRegistryAPI.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers every service the app needs. Tests call this too (via WebApplicationFactory),
    /// which is the point: production and the test host assemble the same container, and a test
    /// only overrides the pieces it deliberately wants to change (e.g. swapping Npgsql for SQLite).
    /// </summary>
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
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

        return builder;
    }
}
