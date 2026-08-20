using Microsoft.EntityFrameworkCore;

namespace AeonRegistryAPI.Services.ArtifactMedia;

public class ArtifactMediaService(ApplicationDbContext db) : IArtifactMediaService
{
    public async Task<PublicArtifactMediaResponse?> GetPublicArtifactImageByIdAsync(int mediaFileId,
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

        return new PublicArtifactMediaResponse
        {
            Data = image.Data,
            ContentType = image.ContentType
        };
    }
}