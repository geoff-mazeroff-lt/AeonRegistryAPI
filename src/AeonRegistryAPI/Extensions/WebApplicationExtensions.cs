using AeonRegistryAPI.Endpoints.Artifact;
using AeonRegistryAPI.Endpoints.CustomIdentity;
using AeonRegistryAPI.Endpoints.Home;
using AeonRegistryAPI.Endpoints.Site;
using AeonRegistryAPI.Middleware;
using AeonRegistryAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace AeonRegistryAPI.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// The middleware pipeline, in order. Kept separate from endpoint mapping so a test host
    /// gets the identical pipeline (auth, static files, the identity-endpoint block) without
    /// Program.cs having to be re-run in a different shape.
    /// </summary>
    public static WebApplication UseApplicationPipeline(this WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseStaticFiles(); // needed for images
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseMiddleware<BlockIdentityEndpoints>();

        return app;
    }

    public static WebApplication MapApplicationEndpoints(this WebApplication app)
    {
        // Map API endpoints for login, logout, etc. using ASP.NET identity
        var authRouteGroup = app.MapGroup("/api/auth")
            .WithTags("Admin - Public");
        authRouteGroup.MapIdentityApi<ApplicationUser>();

        // Map custom endpoints
        app.MapHomeEndpoints();
        app.MapCustomIdentityEndpoints();
        app.MapSiteEndpoints();
        app.MapArtifactMediaFileEndpoints();
        app.MapArtifactEndpoints();

        return app;
    }
}
