using System.Text;

namespace AeonRegistryAPI.Helpers;

public static class ImageValidationHelper
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    ];

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public static async Task ValidateImageAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            throw new InvalidOperationException("File cannot be empty.");
        
        if (file.Length > MaxFileSize)
            throw new InvalidOperationException("File cannot exceed 5 MB.");
        
        if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            throw new InvalidOperationException($"Only image files (JPEG, PNG, GIF, WEBP) are allowed.");
        
        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension.ToLowerInvariant()))
            throw new InvalidOperationException("Unsupported image extension.");

        var header = new byte[8];
        await using (var stream = file.OpenReadStream())
        {
            _ = await stream.ReadAsync(header, cancellationToken);
        }
        
        if (!IsValidImageHeader(header))
            throw new InvalidOperationException("The uploaded file is not a valid image.");
    }

    private static bool IsValidImageHeader(byte[] header)
    {
        // JPEG
        if (header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff)
            return true;
        
        // PNG
        if (header.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }))
            return true;
        
        // GIF8
        if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38)
            return true;
        
        // WEBP
        if (Encoding.ASCII.GetString(header.Take(4).ToArray()) == "RIFF")
            return true;

        return false;
    }
}