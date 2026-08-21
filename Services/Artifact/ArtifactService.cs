using AeonRegistryAPI.Enums;
using AeonRegistryAPI.Models.Request;
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

    public async Task<List<PublicArtifactResponse>> GetPublicArtifactsBySiteAsync(int siteId,
        CancellationToken cancellationToken)
    {
        var siteExists = await db.Sites.AnyAsync(s => s.Id == siteId, cancellationToken);
        if (!siteExists)
            return [];

        return await db.Artifacts
            .AsNoTracking()
            .Include(a => a.Site)
            .Include(a => a.MediaFiles)
            .Where(a => a.SiteId == siteId)
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
                }
            ).ToListAsync(cancellationToken);
    }
    
    public async Task<List<PrivateArtifactResponse>> GetPrivateArtifactsBySiteAsync(int siteId,
        CancellationToken cancellationToken)
    {
        var siteExists = await db.Sites.AnyAsync(s => s.Id == siteId, cancellationToken);
        if (!siteExists)
            return [];

        return await db.Artifacts
            .AsNoTracking()
            .Include(a => a.Site)
            .Include(a => a.MediaFiles)
            .Where(a => a.SiteId == siteId)
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
                }
            ).ToListAsync(cancellationToken);
    }

    public async Task<PrivateArtifactResponse?> CreateArtifactAsync(CreateArtifactRequest request,
        CancellationToken cancellationToken)
    {
        var site = await db.Sites.FindAsync([request.SiteId], cancellationToken);
        if (site is null)
        {
            return null; 
        }

        if (!Enum.TryParse<ArtifactType>(request.Type, true, out _))
        {
            throw new ArgumentException("Invalid artifact type");
        }

        var artifact = new Models.Artifact
        {
            Name = request.Name,
            CatalogNumber = request.CatalogNumber,
            Description = request.Description,
            PublicNarrative = request.PublicNarrative,
            DateDiscovered = request.DateDiscovered,
            Type = request.Type,
            SiteId = request.SiteId,
        };
        
        db.Artifacts.Add(artifact);
        await db.SaveChangesAsync(cancellationToken);

        return new PrivateArtifactResponse
        {
            Id = artifact.Id,
            Name = artifact.Name,
            CatalogNumber = artifact.CatalogNumber,
            Description = artifact.PublicNarrative,
            PrivateDescription = artifact.Description,
            DateDiscovered = artifact.DateDiscovered,
            Type = artifact.Type,
            SiteName = site.Name
        };
    }
}