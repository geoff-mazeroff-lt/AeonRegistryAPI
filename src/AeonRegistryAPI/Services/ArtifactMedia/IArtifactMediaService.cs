namespace AeonRegistryAPI.Services.ArtifactMedia;

public interface IArtifactMediaService
{
    Task<PublicArtifactImageResponse?> GetPublicArtifactImageByIdAsync(int mediaFileId,
        CancellationToken cancellationToken);
    
    Task<ArtifactMediaFileResponse?> CreateArtifactMediaFileAsync(int artifactId,
        IFormFile file, bool isPrimary, CancellationToken cancellationToken);
}