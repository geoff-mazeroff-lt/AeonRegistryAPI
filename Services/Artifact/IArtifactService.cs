namespace AeonRegistryAPI.Services.Artifact;

public interface IArtifactService
{
    Task<List<PublicArtifactResponse>> GetPublicArtifactsAsync(CancellationToken cancellationToken);
    
    Task<List<PrivateArtifactResponse>> GetPrivateArtifactsAsync(CancellationToken cancellationToken);
}