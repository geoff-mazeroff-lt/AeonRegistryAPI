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
}