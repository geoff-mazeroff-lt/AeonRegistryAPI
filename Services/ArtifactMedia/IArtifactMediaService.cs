namespace AeonRegistryAPI.Services.ArtifactMedia;

public interface IArtifactMediaService
{
    Task<PublicArtifactMediaResponse?> GetPublicArtifactImageByIdAsync(int mediaFileId, CancellationToken cancellationToken);
}