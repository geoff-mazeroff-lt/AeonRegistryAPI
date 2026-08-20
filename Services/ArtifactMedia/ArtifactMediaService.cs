using AeonRegistryAPI.Helpers;
using AeonRegistryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AeonRegistryAPI.Services.ArtifactMedia;

public class ArtifactMediaService(ApplicationDbContext db) : IArtifactMediaService
{
    public async Task<PublicArtifactImageResponse?> GetPublicArtifactImageByIdAsync(int mediaFileId,
        CancellationToken cancellationToken)
    {
        var image = await db.ArtifactMediaFiles
            .AsNoTracking()
            .Where(m => m.IsPrimary)
            .FirstOrDefaultAsync(m => m.Id == mediaFileId, cancellationToken);

        if (image is null || image.Data.Length == 0)
        {
            return null;
        }

        return new PublicArtifactImageResponse
        {
            Data = image.Data,
            ContentType = image.ContentType
        };
    }

    public async Task<ArtifactMediaFileResponse?> CreateArtifactMediaFileAsync(int artifactId,
        IFormFile file, bool isPrimary, CancellationToken cancellationToken)
    {
        var artifact = await db.Artifacts.FindAsync([artifactId], cancellationToken);
        if (artifact is null)
            return null;

        await ImageValidationHelper.ValidateImageAsync(file, cancellationToken);
        
        // If this file is primary, ensure all other images for this artifact are not primary.
        if (isPrimary)
        {
            var primaryImages = await db.ArtifactMediaFiles
                .Where(m => m.ArtifactId == artifactId && m.IsPrimary)
                .ToListAsync(cancellationToken);

            foreach (var image in primaryImages)
                image.IsPrimary = false;
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var imageData = memoryStream.ToArray();

        var mediaFileEntity = new ArtifactMediaFile
        {
            ArtifactId = artifactId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Data = imageData,
            IsPrimary = isPrimary
        };

        db.ArtifactMediaFiles.Add(mediaFileEntity);
        await db.SaveChangesAsync(cancellationToken);

        return new ArtifactMediaFileResponse
        {
            Id = mediaFileEntity.Id,
        };
    }
}