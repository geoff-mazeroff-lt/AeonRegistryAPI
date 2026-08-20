using AeonRegistryAPI.Filters;
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

        return route;
    }
    
    private static async Task<Results<Ok<List<PublicArtifactResponse>>, NotFound>> GetPublicArtifactsAsync(
        IArtifactService service, CancellationToken cancellationToken)
    {
        var artifacts = await service.GetPublicArtifactsAsync(cancellationToken);
        if (artifacts.Count == 0)
            return TypedResults.NotFound();
        
        return TypedResults.Ok(artifacts);
    }
    
    private static async Task<Results<Ok<List<PrivateArtifactResponse>>, NotFound>> GetPrivateArtifactsAsync(
        IArtifactService service, CancellationToken cancellationToken)
    {
        var artifacts = await service.GetPrivateArtifactsAsync(cancellationToken);
        if (artifacts.Count == 0)
            return TypedResults.NotFound();
        
        return TypedResults.Ok(artifacts);
    }
}