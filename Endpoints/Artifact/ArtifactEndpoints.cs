using AeonRegistryAPI.Filters;
using AeonRegistryAPI.Models.Request;
using AeonRegistryAPI.Services.Artifact;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AeonRegistryAPI.Endpoints.Artifact;

public static class ArtifactEndpoints
{
    public static IEndpointRouteBuilder MapArtifactEndpoints(this IEndpointRouteBuilder route)
    {
        var publicGroup = route.MapGroup("/api/public/artifacts")
            .WithTags("Artifacts - Public")
            .AddEndpointFilter<ExceptionHandlingFilter>()
            .AllowAnonymous();

        publicGroup.MapGet("", GetPublicArtifactsAsync)
            .WithName("GetPublicArtifacts")
            .WithSummary("Get all public artifacts")
            .WithDescription("Get all public artifacts")
            .Produces<List<PublicArtifactResponse>>()
            .Produces<NotFound>();
        
        publicGroup.MapGet("{id:int}", GetPublicArtifactByIdAsync)
            .WithName("GetPublicArtifactById")
            .WithSummary("Get an artifact by ID (public info only)")
            .WithDescription("Get public info about a specific artifact")
            .Produces<List<PublicArtifactResponse>>()
            .Produces<NotFound>();
        
        var privateGroup = route.MapGroup("/api/private/artifacts")
            .WithTags("Artifacts - Private")
            .AddEndpointFilter<ExceptionHandlingFilter>()
            .RequireAuthorization();
        
        privateGroup.MapGet("", GetPrivateArtifactsAsync)
            .WithName("GetPrivateArtifacts")
            .WithSummary("Get all artifacts (public and private info)")
            .WithDescription("Get all artifacts with full info")
            .Produces<List<PrivateArtifactResponse>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<NotFound>();
        
        privateGroup.MapGet("{id:int}", GetPrivateArtifactByIdAsync)
            .WithName("GetPrivateArtifactById")
            .WithSummary("Get an artifact by ID (public and private info)")
            .WithDescription("Get all info about a specific artifact")
            .Produces<PrivateArtifactResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<NotFound>();
        
        privateGroup.MapPost("", CreatePrivateArtifactAsync)
            .WithName("CreatePrivateArtifact")
            .WithSummary("Create a new artifact")
            .WithDescription("Create a new artifact")
            .ProducesValidationProblem()
            .Produces<PrivateArtifactResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<NotFound>();

        return route;
    }
    
    private static async Task<Results<Ok<List<PublicArtifactResponse>>, NotFound>> GetPublicArtifactsAsync(
        IArtifactService service, CancellationToken cancellationToken)
    {
        var artifacts = await service.GetPublicArtifactsAsync(cancellationToken);
        if (artifacts.Count == 0)
        {
            return TypedResults.NotFound();
        }
        
        return TypedResults.Ok(artifacts);
    }
    
    private static async Task<Results<Ok<PublicArtifactResponse>, NotFound>> GetPublicArtifactByIdAsync(
        int id,
        IArtifactService service,
        CancellationToken cancellationToken)
    {
        var artifact = await service.GetPublicArtifactByIdAsync(id, cancellationToken);
        if (artifact is null)
        {
            return TypedResults.NotFound();
        }
        
        return TypedResults.Ok(artifact);
    }
    
    private static async Task<Results<Ok<List<PrivateArtifactResponse>>, NotFound>> GetPrivateArtifactsAsync(
        IArtifactService service, CancellationToken cancellationToken)
    {
        var artifacts = await service.GetPrivateArtifactsAsync(cancellationToken);
        if (artifacts.Count == 0)
        {
            return TypedResults.NotFound();
        }
        
        return TypedResults.Ok(artifacts);
    }
    
    private static async Task<Results<Ok<PrivateArtifactResponse>, NotFound>> GetPrivateArtifactByIdAsync(
        int id,
        IArtifactService service,
        CancellationToken cancellationToken)
    {
        var artifact = await service.GetPrivateArtifactByIdAsync(id, cancellationToken);
        if (artifact is null)
        {
            return TypedResults.NotFound();
        }
        
        return TypedResults.Ok(artifact);
    }

    private static async Task<Results<Created<PrivateArtifactResponse>, NotFound>> CreatePrivateArtifactAsync(
        CreateArtifactRequest request,
        IArtifactService service, 
        CancellationToken cancellationToken)
    {
        var createdArtifact = await service.CreateArtifactAsync(request, cancellationToken);
        if (createdArtifact is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Created($"/api/private/artifacts/{createdArtifact.Id}", createdArtifact);
    }
}