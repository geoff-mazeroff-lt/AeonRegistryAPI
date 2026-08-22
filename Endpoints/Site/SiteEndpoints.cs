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
            .WithTags("Sites - Public")
            .AddEndpointFilter<ExceptionHandlingFilter>();

        publicGroup.MapGet("", GetAllPublicSitesAsync)
            .WithName("GetAllPublicSites")
            .WithSummary("List all sites")
            .WithDescription("Lists all sites with their public data only.")
            .Produces<IEnumerable<PublicSiteResponse>>();

        publicGroup.MapGet("{id:int}", GetPublicSiteByIdAsync)
            .WithName("GetPublicSiteById")
            .WithSummary("Retrieve a site")
            .WithDescription("Retrieves a site with its public data only.")
            .Produces<PublicSiteResponse>()
            .Produces<NotFound>();

        publicGroup.MapGet("{id:int}/artifacts/", GetPublicArtifactsBySiteAsync)
            .WithName("GetPublicArtifactsBySite")
            .WithSummary("List artifacts for a given site")
            .WithDescription("Lists artifacts for a site with its public data only.")
            .Produces<List<PublicArtifactResponse>>()
            .Produces<NotFound>();
        
        var privateGroup = route.MapGroup("/api/private/sites")
            .WithTags("Sites - Private")
            .RequireAuthorization()
            .AddEndpointFilter<ExceptionHandlingFilter>();

        privateGroup.MapGet("", GetAllPrivateSitesAsync)
            .WithName("GetAllPrivateSites")
            .WithSummary("List all sites")
            .WithDescription("Lists all sites.")
            .Produces<IEnumerable<PrivateSiteResponse>>()
            .Produces(StatusCodes.Status401Unauthorized);

        privateGroup.MapGet("{id:int}", GetPrivateSiteByIdAsync)
            .WithName("GetPrivateSiteById")
            .WithSummary("Retrieve a site")
            .WithDescription("Retrieves a site.")
            .Produces<PrivateSiteResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<NotFound>();

        privateGroup.MapPost("", CreatePrivateSiteAsync)
            .WithName("CreatePrivateSite")
            .WithSummary("Create a site")
            .WithDescription("Creates a new site.")
            .Produces<PrivateSiteResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();

        privateGroup.MapPut("", UpdatePrivateSiteAsync)
            .WithName("UpdatePrivateSite")
            .WithSummary("Update a site")
            .WithDescription("Updates an existing site.")
            .ProducesValidationProblem()
            .Produces<NoContent>()
            .Produces<NotFound>()
            .Produces(StatusCodes.Status401Unauthorized);

        privateGroup.MapDelete("", DeletePrivateSiteAsync)
            .WithName("DeletePrivateSite")
            .WithSummary("Delete a site")
            .WithDescription("Deletes a site.")
            .Produces<NoContent>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<NotFound>();

        privateGroup.MapGet("{id:int}/artifacts/", GetPrivateArtifactsBySiteAsync)
            .WithName("GetPrivateArtifactsBySite")
            .WithSummary("List artifacts for a given site")
            .WithDescription("Lists artifacts for a site.")
            .Produces<List<PrivateArtifactResponse>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<NotFound>();
        
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

    private static async Task<Results<Ok<List<PublicArtifactResponse>>, NotFound>> GetPublicArtifactsBySiteAsync(int id,
        IArtifactService service, CancellationToken cancellationToken)
    {
        var artifacts = await service.GetPublicArtifactsBySiteAsync(id, cancellationToken);

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
    
    private static async Task<Results<Ok<List<PrivateArtifactResponse>>, NotFound>> GetPrivateArtifactsBySiteAsync(int id,
        IArtifactService service, CancellationToken cancellationToken)
    {
        var artifacts = await service.GetPrivateArtifactsBySiteAsync(id, cancellationToken);

        if (artifacts.Count == 0)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(artifacts);
    }
}