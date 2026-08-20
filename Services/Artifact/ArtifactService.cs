using Microsoft.EntityFrameworkCore;

namespace AeonRegistryAPI.Services.Artifact;

public class ArtifactService(ApplicationDbContext db) : IArtifactService
{
    public async Task<List<PublicArtifactResponse>> GetPublicArtifactsAsync(CancellationToken cancellationToken)
    {
        return await db.Artifacts
            .AsNoTracking()
            .Include(a => a.Site)
            .Include(a => a.MediaFiles)
            .Select(a => new PublicArtifactResponse
            {
                Id = a.Id,
                Name = a.Name,
                CatalogNumber = a.CatalogNumber,
                Description = a.PublicNarrative,
                DateDiscovered = a.DateDiscovered,
                Type = a.Type,
                SiteName = a.Site != null ? a.Site.Name : string.Empty,
                PrimaryImageUrl = a.MediaFiles
                    .Where(m => m.IsPrimary)
                    .Select(m => $"/api/public/artifacts/images/{m.Id}")
                    .FirstOrDefault()
            }).ToListAsync(cancellationToken);
    }
    
    public async Task<List<PrivateArtifactResponse>> GetPrivateArtifactsAsync(CancellationToken cancellationToken)
    {
        return await db.Artifacts
            .AsNoTracking()
            .Include(a => a.Site)
            .Include(a => a.MediaFiles)
            .Select(a => new PrivateArtifactResponse
            {
                Id = a.Id,
                Name = a.Name,
                CatalogNumber = a.CatalogNumber,
                Description = a.PublicNarrative,
                PrivateDescription = a.Description,
                DateDiscovered = a.DateDiscovered,
                Type = a.Type,
                SiteName = a.Site != null ? a.Site.Name : string.Empty,
                PrimaryImageUrl = a.MediaFiles
                    .Where(m => m.IsPrimary)
                    .Select(m => $"/api/public/artifacts/images/{m.Id}")
                    .FirstOrDefault()
            }).ToListAsync(cancellationToken);
    }
}