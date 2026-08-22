using AeonRegistryAPI.Filters;
using AeonRegistryAPI.Models.Request;
using AeonRegistryAPI.Services.Artifact;
using AeonRegistryAPI.Services.Site;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AeonRegistryAPI.Endpoints.Site;

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
            .WithDescription("Get all sites with their public data only");

        publicGroup.MapGet("{id:int}", GetPublicSiteByIdAsync)
            .WithName("GetPublicSiteById")
            .Produces<PublicSiteResponse>()
            .Produces<NotFound>()
            .WithSummary("Get site by ID (public)")
            .WithDescription("Get a site by ID with its public data only");

        publicGroup.MapGet("{siteId:int}/artifacts/", GetPublicArtifactsBySiteAsync)
            .WithName("GetPublicArtifactsBySite")
            .Produces<List<PublicArtifactResponse>>()
            .Produces<NotFound>()
            .WithSummary("Get artifacts at a given site ID (public)")
            .WithDescription("Get artifacts at a site with its public data only");
        
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

        privateGroup.MapGet("{id:int}", GetPrivateSiteByIdAsync)
            .WithName("GetPrivateSiteById")
            .Produces<PrivateSiteResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<NotFound>()
            .WithSummary("Get site by ID (public and private)")
            .WithDescription("Get a site by ID with its public and private data");

        privateGroup.MapPost("", CreatePrivateSiteAsync)
            .WithName("CreatePrivateSite")
            .Accepts<CreateSiteRequest>("application/json")
            .Produces<PrivateSiteResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem()
            .WithSummary("Create a new site")
            .WithDescription("Create a new site and return its details");
        
        privateGroup.MapPut("", UpdatePrivateSiteAsync)
            .WithName("UpdatePrivateSite")
            .Accepts<UpdateSiteRequest>("application/json")
            .Produces<NoContent>()
            .Produces<NotFound>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem()
            .WithSummary("Update an existing site")
            .WithDescription("Update an existing site and return its updated details");
        
        privateGroup.MapDelete("", DeletePrivateSiteAsync)
            .WithName("DeletePrivateSite")
            .Accepts<UpdateSiteRequest>("application/json")
            .Produces<NoContent>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<NotFound>()
            .ProducesValidationProblem()
            .WithSummary("Delete an existing site")
            .WithDescription("Delete an existing site");
        
        privateGroup.MapGet("{siteId:int}/artifacts/", GetPrivateArtifactsBySiteAsync)
            .WithName("GetPrivateArtifactsBySite")
            .Produces<List<PrivateArtifactResponse>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<NotFound>()
            .WithSummary("Get artifacts at a given site (public and private)")
            .WithDescription("Get artifacts at a given site ID with all data")
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

    private static async Task<Results<Ok<List<PublicArtifactResponse>>, NotFound>> GetPublicArtifactsBySiteAsync(int siteId,
        IArtifactService service, CancellationToken cancellationToken)
    {
        var artifacts = await service.GetPublicArtifactsBySiteAsync(siteId, cancellationToken);

        if (artifacts.Count == 0)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(artifacts);
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

    private static async Task<Results<Created<PrivateSiteResponse>, ValidationProblem>> CreatePrivateSiteAsync(
        CreateSiteRequest request, ISiteService service, CancellationToken cancellationToken)
    {
        var createdSite = await service.CreateSiteAsync(request, cancellationToken);
        return TypedResults.Created($"/api/private/sites/{createdSite.Id}", createdSite);
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdatePrivateSiteAsync(
        int id, UpdateSiteRequest request, ISiteService service, CancellationToken cancellationToken)
    {
        var wasUpdated = await service.UpdateSiteAsync(id, request, cancellationToken);
        return !wasUpdated ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> DeletePrivateSiteAsync(int id,
        ISiteService service, CancellationToken cancellationToken)
    {
        var wasDeleted = await service.DeleteSiteAsync(id, cancellationToken);
        return !wasDeleted ? TypedResults.NotFound() : TypedResults.NoContent();
    }
    
    private static async Task<Results<Ok<List<PrivateArtifactResponse>>, NotFound>> GetPrivateArtifactsBySiteAsync(int siteId,
        IArtifactService service, CancellationToken cancellationToken)
    {
        var artifacts = await service.GetPrivateArtifactsBySiteAsync(siteId, cancellationToken);

        if (artifacts.Count == 0)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(artifacts);
    }
}