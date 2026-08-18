namespace AeonRegistryAPI.Services.Site;

public interface ISiteService
{
    Task<IEnumerable<PublicSiteResponse>> GetAllPublicSitesAsync(CancellationToken cancellationToken);
    
    Task<PublicSiteResponse?> GetPublicSiteByIdAsync(int siteId, CancellationToken cancellationToken);
    
    Task<IEnumerable<PrivateSiteResponse>> GetAllPrivateSitesAsync(CancellationToken cancellationToken);
}