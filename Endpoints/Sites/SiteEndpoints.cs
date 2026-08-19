using AeonRegistryAPI.Filters;
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
            .WithTags("Sites - Public")
            .AddEndpointFilter<ExceptionHandlingFilter>();

        publicGroup.MapGet("", GetAllPublicSitesAsync)
            .WithName("GetAllPublicSites")
            .Produces<IEnumerable<PublicSiteResponse>>()
            .WithSummary("Get all sites (public)")
            .WithDescription("Get all sites with their public data only")
            .AllowAnonymous();
        
        publicGroup.MapGet("/{id:int}", GetPublicSiteByIdAsync)
            .WithName("GetPublicSiteById")
            .Produces<PublicSiteResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Get site by ID (public)")
            .WithDescription("Get a site by ID with its public data only")
            .AllowAnonymous();
        
        var privateGroup = route.MapGroup("/api/private/sites")
            .RequireAuthorization()
            .WithSummary("Private Site Endpoints")
            .WithDescription("Endpoints that expose public and private site data")
            .WithTags("Sites - Private")
            .AddEndpointFilter<ExceptionHandlingFilter>();

        privateGroup.MapGet("", GetAllPrivateSitesAsync)
            .WithName("GetAllPrivateSites")
            .Produces<IEnumerable<PrivateSiteResponse>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .WithSummary("Get all sites (private)")
            .WithDescription("Get all sites with their public and private data");
        
        privateGroup.MapGet("/{id:int}", GetPrivateSiteByIdAsync)
            .WithName("GetPrivateSiteById")
            .Produces<PrivateSiteResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Get site by ID (public and private)")
            .WithDescription("Get a site by ID with its public and private data")
            .AllowAnonymous();

        return route;
    }
    
    private static async Task<Ok<IEnumerable<PublicSiteResponse>>> GetAllPublicSitesAsync(ISiteService service,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await service.GetAllPublicSitesAsync(cancellationToken));
    }

    private static async Task<Results<Ok<PublicSiteResponse>, NotFound>> GetPublicSiteByIdAsync(
        int id,
        ISiteService service,
        CancellationToken cancellationToken)
    {
        var site = await service.GetPublicSiteByIdAsync(id, cancellationToken);
        return site is null ? TypedResults.NotFound() : TypedResults.Ok(site);
    }

    private static async Task<Ok<IEnumerable<PrivateSiteResponse>>> GetAllPrivateSitesAsync(ISiteService service,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await service.GetAllPrivateSitesAsync(cancellationToken));
    }
    
    private static async Task<Results<Ok<PrivateSiteResponse>, NotFound>> GetPrivateSiteByIdAsync(
        int id,
        ISiteService service,
        CancellationToken cancellationToken)
    {
        var site = await service.GetPrivateSiteByIdAsync(id, cancellationToken);
        return site is null ? TypedResults.NotFound() : TypedResults.Ok(site);
    }
}