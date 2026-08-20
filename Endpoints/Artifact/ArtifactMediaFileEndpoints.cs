using AeonRegistryAPI.Filters;
using AeonRegistryAPI.Services.ArtifactMedia;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AeonRegistryAPI.Endpoints.Artifact;

public static class ArtifactMediaFileEndpoints
{
    public static IEndpointRouteBuilder MapArtifactMediaFileEndpoints(this IEndpointRouteBuilder route)
    {
        var publicGroup = route.MapGroup("/api/public/artifacts/images")
            .WithTags("Artifact Media - Public")
            .AddEndpointFilter<ExceptionHandlingFilter>()
            .AllowAnonymous();

        publicGroup.MapGet("/{id:int}", GetPublicArtifactImage)
            .WithName("GetPublicArtifactImage")
            .Produces<FileContentHttpResult>(StatusCodes.Status200OK)
            .Produces<NotFound>()
            .WithSummary("Get artifact image (public)")
            .WithDescription("Get binary image data for a specific artifact media record (public)");
        
        return route;
    }
    
    private static async Task<Results<FileContentHttpResult, NotFound>> GetPublicArtifactImage(int id,
        IArtifactMediaService service,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var artifactImageResponse = await service.GetPublicArtifactImageByIdAsync(id, cancellationToken);
        if (artifactImageResponse is null)
        {
            return TypedResults.NotFound();
        }

        response.Headers.CacheControl = "public, max-age=86400";
        
        return TypedResults.File(artifactImageResponse.Data, artifactImageResponse.ContentType);
    }
}