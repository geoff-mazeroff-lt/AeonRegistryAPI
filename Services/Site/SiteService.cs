using Microsoft.EntityFrameworkCore;

namespace AeonRegistryAPI.Services.Site;

public class SiteService(ApplicationDbContext db) : ISiteService
{
    public async Task<IEnumerable<PublicSiteResponse>> GetAllPublicSitesAsync(CancellationToken cancellationToken = default)
    {
        return await db.Sites
            .AsNoTracking()
            .Select(site => new PublicSiteResponse
            {
                Id = site.Id,
                Name = site.Name,
                Location = site.Location,
                Coordinates = site.Coordinates,
                Latitude = site.Latitude,
                Longitude = site.Longitude,
                Description = site.Description,
                PublicNarrative = site.PublicNarrative
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PublicSiteResponse?> GetPublicSiteByIdAsync(int siteId, CancellationToken cancellationToken)
    {
        return await db.Sites
            .AsNoTracking()
            .Where(s => s.Id == siteId)
            .Select(site => new PublicSiteResponse
            {
                Id = site.Id,
                Name = site.Name,
                Location = site.Location,
                Coordinates = site.Coordinates,
                Latitude = site.Latitude,
                Longitude = site.Longitude,
                Description = site.Description,
                PublicNarrative = site.PublicNarrative
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}