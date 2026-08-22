using AeonRegistryAPI.Models.Request;

namespace AeonRegistryAPI.Services.Artifact;

public interface IArtifactService
{
    Task<List<PublicArtifactResponse>> GetPublicArtifactsAsync(CancellationToken cancellationToken);
    
    Task<PublicArtifactResponse?> GetPublicArtifactByIdAsync(int id, CancellationToken cancellationToken);
    
    Task<List<PublicArtifactResponse>> GetPublicArtifactsBySiteAsync(int siteId, CancellationToken cancellationToken);
    
    Task<List<PrivateArtifactResponse>> GetPrivateArtifactsAsync(CancellationToken cancellationToken);
    
    Task<PrivateArtifactResponse?> GetPrivateArtifactByIdAsync(int id, CancellationToken cancellationToken);
    
    Task<List<PrivateArtifactResponse>> GetPrivateArtifactsBySiteAsync(int siteId, CancellationToken cancellationToken);
    
    Task<PrivateArtifactResponse?> CreateArtifactAsync(CreateArtifactRequest request, CancellationToken cancellationToken);
    
    Task<bool> UpdateArtifactAsync(int id, UpdateArtifactRequest request, CancellationToken cancellationToken);
}