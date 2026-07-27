namespace OVCMOVE.Api.Common;

/// <summary>Validates image metadata and file signatures at the HTTP boundary.</summary>
public static class ImageFileValidator
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly byte[] PngSignature =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Returns a user-facing validation error, or null for a supported image.</summary>
    public static async Task<string?> ValidateAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return "Vui lòng chọn một file ảnh để tải lên.";
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return "File ảnh vượt quá giới hạn 5MB.";
        }

        var signature = new byte[12];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAtLeastAsync(
            signature,
            signature.Length,
            throwOnEndOfStream: false,
            cancellationToken);

        return MatchesDeclaredType(
            file.ContentType,
            signature.AsSpan(0, bytesRead))
            ? null
            : "Nội dung file không đúng định dạng JPG, PNG hoặc WEBP.";
    }

    private static bool MatchesDeclaredType(
        string contentType,
        ReadOnlySpan<byte> signature) =>
        contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => IsJpeg(signature),
            "image/png" => IsPng(signature),
            "image/webp" => IsWebp(signature),
            _ => false
        };

    private static bool IsJpeg(ReadOnlySpan<byte> signature) =>
        signature.Length >= 3 &&
        signature[0] == 0xFF &&
        signature[1] == 0xD8 &&
        signature[2] == 0xFF;

    private static bool IsPng(ReadOnlySpan<byte> signature) =>
        signature.Length >= 8 &&
        signature[..8].SequenceEqual(PngSignature);

    private static bool IsWebp(ReadOnlySpan<byte> signature) =>
        signature.Length >= 12 &&
        signature[..4].SequenceEqual("RIFF"u8) &&
        signature[8..12].SequenceEqual("WEBP"u8);
}
