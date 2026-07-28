using Microsoft.AspNetCore.Http;
using OVCMOVE.Api.Common;

namespace OVCMOVE.Test.Application;

public class ImageFileValidatorTests
{
    [Fact]
    public async Task ValidateAsync_AcceptsMatchingPngSignature()
    {
        var file = CreateFile(
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
            "image/png");

        var error = await ImageFileValidator.ValidateAsync(
            file,
            CancellationToken.None);

        Assert.Null(error);
    }

    [Fact]
    public async Task ValidateAsync_RejectsSpoofedContentType()
    {
        var file = CreateFile("not-a-png"u8.ToArray(), "image/png");

        var error = await ImageFileValidator.ValidateAsync(
            file,
            CancellationToken.None);

        Assert.NotNull(error);
    }

    private static FormFile CreateFile(
        byte[] content,
        string contentType)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", "image")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
