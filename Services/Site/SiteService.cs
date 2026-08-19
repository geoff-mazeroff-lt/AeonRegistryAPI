using AeonRegistryAPI.Models.Request;
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

    public async Task<IEnumerable<PrivateSiteResponse>> GetAllPrivateSitesAsync(CancellationToken cancellationToken)
    {
        return await db.Sites
            .AsNoTracking()
            .Select(site => new PrivateSiteResponse
            {
                Id = site.Id,
                Name = site.Name,
                Location = site.Location,
                Coordinates = site.Coordinates,
                Latitude = site.Latitude,
                Longitude = site.Longitude,
                Description = site.Description,
                PublicNarrative = site.PublicNarrative,
                AeonNarrative = site.AeonNarrative
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PrivateSiteResponse?> GetPrivateSiteByIdAsync(int siteId, CancellationToken cancellationToken)
    {
        return await db.Sites
            .AsNoTracking()
            .Where(s => s.Id == siteId)
            .Select(site => new PrivateSiteResponse
            {
                Id = site.Id,
                Name = site.Name,
                Location = site.Location,
                Coordinates = site.Coordinates,
                Latitude = site.Latitude,
                Longitude = site.Longitude,
                Description = site.Description,
                PublicNarrative = site.PublicNarrative,
                AeonNarrative = site.AeonNarrative
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PrivateSiteResponse> CreateSiteAsync(CreateSiteRequest request,
        CancellationToken cancellationToken)
    {
        var site = new Models.Site
        {
            Name = request.Name,
            Location = request.Location,
            Coordinates = request.Coordinates,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Description = request.Description,
            PublicNarrative = request.PublicNarrative,
            AeonNarrative = request.AeonNarrative
        };
        
        db.Sites.Add(site);
        await db.SaveChangesAsync(cancellationToken);

        return new PrivateSiteResponse
        {
            Id = site.Id,
            Name = site.Name,
            Location = site.Location,
            Coordinates = site.Coordinates,
            Latitude = site.Latitude,
            Longitude = site.Longitude,
            Description = site.Description,
            PublicNarrative = site.PublicNarrative,
            AeonNarrative = site.AeonNarrative
        };
    }
}