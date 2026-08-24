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

    public async Task<PublicArtifactResponse?> GetPublicArtifactByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await db.Artifacts
            .AsNoTracking()
            .Include(a => a.Site)
            .Include(a => a.MediaFiles)
            .Where(a => a.Id == id)
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
            }).FirstOrDefaultAsync(cancellationToken);
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
    
    public async Task<PrivateArtifactResponse?> GetPrivateArtifactByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await db.Artifacts
            .AsNoTracking()
            .Include(a => a.Site)
            .Include(a => a.MediaFiles)
            .Where(a => a.Id == id)
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
            }).FirstOrDefaultAsync(cancellationToken);
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

    public async Task<bool> UpdateArtifactAsync(int id, UpdateArtifactRequest request,
        CancellationToken cancellationToken)
    {
        var siteExists = await db.Sites.AnyAsync(s => s.Id == request.SiteId, cancellationToken);
        if (!siteExists)
        {
            return false;
        }
        
        var existingArtifact = await db.Artifacts.FindAsync([id], cancellationToken);
        if (existingArtifact is null)
        {
            return false;
        }
        
        if (!Enum.TryParse<ArtifactType>(request.Type, true, out _))
        {
            throw new ArgumentException("Invalid artifact type");
        }

        existingArtifact.Name = request.Name;
        existingArtifact.CatalogNumber = request.CatalogNumber;
        existingArtifact.Description = request.Description;
        existingArtifact.PublicNarrative = request.PublicNarrative;
        existingArtifact.DateDiscovered = request.DateDiscovered;
        existingArtifact.Type = request.Type;
        
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteArtifactAsync(int id, CancellationToken cancellationToken)
    {
        var existingArtifact = await db.Artifacts.FindAsync([id], cancellationToken);
        if (existingArtifact is null)
        {
            return false;
        }

        if (existingArtifact.MediaFiles is not null && existingArtifact.MediaFiles.Count != 0)
        {
            db.ArtifactMediaFiles.RemoveRange(existingArtifact.MediaFiles);
        }

        db.Artifacts.Remove(existingArtifact);
        await db.SaveChangesAsync(cancellationToken);
        
        return true;
    }
}