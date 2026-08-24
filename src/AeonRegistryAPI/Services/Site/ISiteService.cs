using AeonRegistryAPI.Models.Request;

namespace AeonRegistryAPI.Services.Site;

public interface ISiteService
{
    Task<IEnumerable<PublicSiteResponse>> GetAllPublicSitesAsync(CancellationToken cancellationToken);
    
    Task<PublicSiteResponse?> GetPublicSiteByIdAsync(int siteId, CancellationToken cancellationToken);
    
    Task<IEnumerable<PrivateSiteResponse>> GetAllPrivateSitesAsync(CancellationToken cancellationToken);
    
    Task<PrivateSiteResponse?> GetPrivateSiteByIdAsync(int siteId, CancellationToken cancellationToken);
    
    Task<PrivateSiteResponse> CreateSiteAsync(CreateSiteRequest request, CancellationToken cancellationToken);
    
    Task<bool> UpdateSiteAsync(int siteId, UpdateSiteRequest request, CancellationToken cancellationToken);
    
    Task<bool> DeleteSiteAsync(int siteId, CancellationToken cancellationToken);
    
    Task<bool> ArchiveSiteAsync(int siteId, CancellationToken cancellationToken);
}