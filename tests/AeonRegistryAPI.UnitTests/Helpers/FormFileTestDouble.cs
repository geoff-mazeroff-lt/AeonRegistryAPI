using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace AeonRegistryAPI.UnitTests.Helpers;

/// <summary>
/// Builds <see cref="IFormFile"/> substitutes for the validation tests.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IFormFile"/> is an interface, which is exactly why <c>ImageValidationHelper</c>
/// is unit-testable at all: there is a seam, so NSubstitute can stand in for the real
/// multipart-form file without a request, a host, or a file on disk. Compare this with the
/// service layer, where the concrete <c>ApplicationDbContext</c> is injected and no such
/// seam exists - those tests have to be database tests.
/// </para>
/// <para>
/// Only the four members the helper actually touches are configured: <c>Length</c>,
/// <c>ContentType</c>, <c>FileName</c>, and <c>OpenReadStream()</c>. Configuring more would
/// make the test lie about what the production code depends on.
/// </para>
/// </remarks>
internal static class FormFileTestDouble
{
    internal static readonly byte[] JpegHeader = [0xff, 0xd8, 0xff, 0xe0];
    internal static readonly byte[] PngHeader = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
    internal static readonly byte[] GifHeader = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61];
    internal static readonly byte[] WebpHeader = [0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00]; // "RIFF"

    /// <summary>
    /// A file that passes every check: JPEG content type, <c>.jpg</c> extension, JPEG magic bytes.
    /// Individual tests override just the one property they are about, so the deviation from
    /// valid is the visible part of the test.
    /// </summary>
    internal static IFormFile Valid() => Create();

    internal static IFormFile Create(
        string? contentType = "image/jpeg",
        string? fileName = "aeon-site.jpg",
        byte[]? content = null,
        long? length = null)
    {
        content ??= JpegHeader;

        var file = Substitute.For<IFormFile>();
        file.ContentType.Returns(contentType);
        file.FileName.Returns(fileName);
        file.Length.Returns(length ?? content.Length);

        // A fresh stream per call: the helper disposes what OpenReadStream() hands it, so a
        // single shared MemoryStream would be unreadable on a second call.
        file.OpenReadStream().Returns(_ => new MemoryStream(content));

        return file;
    }
}
