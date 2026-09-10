using AeonRegistryAPI.Helpers;
using Microsoft.AspNetCore.Http;

namespace AeonRegistryAPI.UnitTests.Helpers;

public class ImageValidationHelperTests
{
    private static Task ValidateAsync(IFormFile file) =>
        ImageValidationHelper.ValidateImageAsync(file, CancellationToken.None);

    // ---------------------------------------------------------------------------------
    // Size
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task ValidateImageAsync_NullFile_ThrowsInvalidOperationException()
    {
        // `null!` because the parameter is declared non-nullable but the method still guards
        // against null - the guard is reachable from any caller that isn't nullable-aware.
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => ValidateAsync(null!));

        exception.Message.ShouldBe("File cannot be empty.");
    }

    [Fact]
    public async Task ValidateImageAsync_ZeroLengthFile_ThrowsInvalidOperationException()
    {
        var file = FormFileTestDouble.Create(length: 0);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => ValidateAsync(file));

        exception.Message.ShouldBe("File cannot be empty.");
    }

    [Fact]
    public async Task ValidateImageAsync_FileExceedingFiveMegabytes_ThrowsInvalidOperationException()
    {
        // One byte over the 5 MB limit
        var file = FormFileTestDouble.Create(length: (5 * 1024 * 1024) + 1);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => ValidateAsync(file));

        exception.Message.ShouldBe("File cannot exceed 5 MB.");
    }

    [Fact]
    public async Task ValidateImageAsync_FileExactlyAtFiveMegabytes_DoesNotThrow()
    {
        var file = FormFileTestDouble.Create(length: 5 * 1024 * 1024);

        await Should.NotThrowAsync(() => ValidateAsync(file));
    }

    // ---------------------------------------------------------------------------------
    // Content type
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/gif")]
    [InlineData("image/webp")]
    [InlineData("IMAGE/JPEG")] // the helper lower-cases before comparing
    public async Task ValidateImageAsync_AllowedContentType_DoesNotThrow(string contentType)
    {
        var file = FormFileTestDouble.Create(contentType: contentType);

        await Should.NotThrowAsync(() => ValidateAsync(file));
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/bmp")]
    [InlineData("image/svg+xml")]
    [InlineData("text/plain")]
    [InlineData("")]
    public async Task ValidateImageAsync_DisallowedContentType_ThrowsInvalidOperationException(string contentType)
    {
        var file = FormFileTestDouble.Create(contentType: contentType);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => ValidateAsync(file));

        exception.Message.ShouldBe("Only image files (JPEG, PNG, GIF, WEBP) are allowed.");
    }

    // ---------------------------------------------------------------------------------
    // Extension
    // ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("site.jpg")]
    [InlineData("site.jpeg")]
    [InlineData("site.JPG")] // lower-cased before comparing
    [InlineData("archive.2026.jpg")] // Path.GetExtension takes the last dot only
    public async Task ValidateImageAsync_AllowedExtension_DoesNotThrow(string fileName)
    {
        var file = FormFileTestDouble.Create(fileName: fileName);

        await Should.NotThrowAsync(() => ValidateAsync(file));
    }

    [Theory]
    [InlineData("site.bmp")]
    [InlineData("site.jpg.exe")]
    [InlineData("site")] // no extension at all: GetExtension returns string.Empty
    public async Task ValidateImageAsync_DisallowedExtension_ThrowsInvalidOperationException(string fileName)
    {
        var file = FormFileTestDouble.Create(fileName: fileName);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => ValidateAsync(file));

        exception.Message.ShouldBe("Unsupported image extension.");
    }

    [Fact]
    public async Task ValidateImageAsync_ExtensionDisagreeingWithContentType_DoesNotThrow()
    {
        // Deliberately pinning a design decision, not a bug: the three checks are independent,
        // so a PNG content type with a .jpg name and JPEG magic bytes passes. The magic-byte
        // check is the one that actually establishes the file is an image; the other two are
        // cheap early rejections. If cross-validation is ever wanted, this test is the one
        // that will fail and say so.
        var file = FormFileTestDouble.Create(
            contentType: "image/png",
            fileName: "site.jpg",
            content: FormFileTestDouble.JpegHeader);

        await Should.NotThrowAsync(() => ValidateAsync(file));
    }

    // ---------------------------------------------------------------------------------
    // Magic bytes
    // ---------------------------------------------------------------------------------

    [Fact]
    public async Task ValidateImageAsync_JpegMagicBytes_DoesNotThrow() =>
        await Should.NotThrowAsync(() => ValidateAsync(FormFileTestDouble.Create(
            contentType: "image/jpeg", fileName: "site.jpg", content: FormFileTestDouble.JpegHeader)));

    [Fact]
    public async Task ValidateImageAsync_PngMagicBytes_DoesNotThrow() =>
        await Should.NotThrowAsync(() => ValidateAsync(FormFileTestDouble.Create(
            contentType: "image/png", fileName: "site.png", content: FormFileTestDouble.PngHeader)));

    [Fact]
    public async Task ValidateImageAsync_GifMagicBytes_DoesNotThrow() =>
        await Should.NotThrowAsync(() => ValidateAsync(FormFileTestDouble.Create(
            contentType: "image/gif", fileName: "site.gif", content: FormFileTestDouble.GifHeader)));

    [Fact]
    public async Task ValidateImageAsync_WebpMagicBytes_DoesNotThrow() =>
        await Should.NotThrowAsync(() => ValidateAsync(FormFileTestDouble.Create(
            contentType: "image/webp", fileName: "site.webp", content: FormFileTestDouble.WebpHeader)));

    [Fact]
    public async Task ValidateImageAsync_ContentThatIsNotAnImage_ThrowsInvalidOperationException()
    {
        // An executable renamed to .jpg and labeled image/jpeg - the case the header check
        // is for. "MZ" is the DOS/PE signature.
        var file = FormFileTestDouble.Create(content: [0x4d, 0x5a, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00]);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => ValidateAsync(file));

        exception.Message.ShouldBe("The uploaded file is not a valid image.");
    }

    [Fact]
    public async Task ValidateImageAsync_TruncatedPngHeader_ThrowsInvalidOperationException()
    {
        // Only the first four of the eight PNG signature bytes. The remaining four stay zero
        // in the read buffer, so SequenceEqual fails and the file is rejected.
        var file = FormFileTestDouble.Create(
            contentType: "image/png",
            fileName: "site.png",
            content: [0x89, 0x50, 0x4e, 0x47]);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => ValidateAsync(file));

        exception.Message.ShouldBe("The uploaded file is not a valid image.");
    }

    [Fact]
    public async Task ValidateImageAsync_SingleContentByte_ThrowsInvalidOperationException()
    {
        // Guards against a regression that the `header[0..3]` indexing looks like it invites.
        // It is not an out-of-bounds risk: `header` is always an eight-byte zero-filled array
        // and ReadAsync leaves the unread tail at zero, so a one-byte file reads as
        // 0x21 followed by seven zeros. Rewriting the check to index the payload directly
        // would break here.
        var file = FormFileTestDouble.Create(content: [0x21]);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => ValidateAsync(file));

        exception.Message.ShouldBe("The uploaded file is not a valid image.");
    }
}
