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

        publicGroup.MapGet("{id:int}", GetPublicArtifactImage)
            .WithName("GetPublicArtifactImage")
            .WithSummary("Retrieve an artifact image")
            .WithDescription("Retrieves the binary image data for an artifact media record.")
            .Produces<FileContentHttpResult>()
            .Produces<NotFound>();
        
        var privateGroup = route.MapGroup("/api/private/artifacts/media-file")
            .WithTags("Artifact Media - Private")
            .RequireAuthorization()
            .AddEndpointFilter<ExceptionHandlingFilter>();

        privateGroup.MapPost("", CreateArtifactMediaFile)
            .WithName("CreateArtifactMediaFile")
            .WithSummary("Create an artifact media file")
            .WithDescription("""
                             Uploads an image file and associates it with an existing artifact.
                             If 'isPrimary' is true, the new image will be set as the primary image for
                             the artifact and any other previous primary image will be be marked as not primary.
                             """)
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery()
            .Produces<Created>()
            .Produces<BadRequest<string>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<NotFound>();
        
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

    private static async Task<Results<Created, NotFound, BadRequest<string>>> CreateArtifactMediaFile(int artifactId,
        IFormFile file,
        bool isPrimary,
        IArtifactMediaService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var createResponse =
                await service.CreateArtifactMediaFileAsync(artifactId, file, isPrimary, cancellationToken);

            if (createResponse is null)
                return TypedResults.NotFound();

            var location = $"/api/public/artifacts/images/{createResponse.Id}";
            return TypedResults.Created(location);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }
}