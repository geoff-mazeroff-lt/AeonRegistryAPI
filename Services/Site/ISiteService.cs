namespace AeonRegistryAPI.Services.Site;

public interface ISiteService
{
    Task<IEnumerable<PublicSiteResponse>> GetAllPublicSitesAsync(CancellationToken cancellationToken);
}