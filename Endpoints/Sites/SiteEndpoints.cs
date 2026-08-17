using AeonRegistryAPI.Services.Site;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AeonRegistryAPI.Endpoints.Sites;

public static class SiteEndpoints
{
    public static IEndpointRouteBuilder MapSiteEndpoints(this IEndpointRouteBuilder route)
    {
        var publicGroup = route.MapGroup("/api/public/sites")
            .AllowAnonymous()
            .WithSummary("Public Site Endpoints")
            .WithDescription("Endpoints that expose public site data")
            .WithTags("Sites - Public");

        publicGroup.MapGet("", GetAllPublicSitesAsync)
            .WithName("GetAllPublicSites")
            .Produces<IEnumerable<PublicSiteResponse>>(StatusCodes.Status200OK)
            .WithSummary("Get all sites (public)")
            .WithDescription("Get all sites with their public data only");

        return route;
    }
    
    private static async Task<Ok<IEnumerable<PublicSiteResponse>>> GetAllPublicSitesAsync(ISiteService service,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await service.GetAllPublicSitesAsync(cancellationToken));
    }
}