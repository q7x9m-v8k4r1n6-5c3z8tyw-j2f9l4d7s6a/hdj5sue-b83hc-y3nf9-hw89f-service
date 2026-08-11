using Microsoft.AspNetCore.Http;

namespace OVCMOVE2026.Plugin.Common;

/// <summary>Validates image and video metadata and file signatures at the HTTP boundary.</summary>
public static class MediaFileValidator
{
    public const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5MB
    public const long MaxVideoSizeBytes = 100 * 1024 * 1024; // 100MB an toàn

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static async Task<string?> ValidateImageAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return "Vui lòng chọn file ảnh.";
        if (file.Length > MaxImageSizeBytes) return "File ảnh vượt quá giới hạn 5MB.";

        var signature = new byte[12];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAtLeastAsync(signature, signature.Length, throwOnEndOfStream: false, cancellationToken);

        return MatchesImageDeclaredType(file.ContentType, signature.AsSpan(0, bytesRead))
            ? null
            : "Nội dung file không đúng định dạng JPG, PNG hoặc WEBP.";
    }

    public static async Task<string?> ValidateVideoAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return "Vui lòng chọn file video.";
        if (file.Length > MaxVideoSizeBytes) return "File video vượt quá giới hạn 100MB.";

        var signature = new byte[12];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAtLeastAsync(signature, signature.Length, throwOnEndOfStream: false, cancellationToken);

        return MatchesVideoDeclaredType(file.ContentType, signature.AsSpan(0, bytesRead))
            ? null
            : "Nội dung file video không đúng định dạng MP4 hoặc MOV.";
    }

    private static bool MatchesImageDeclaredType(string contentType, ReadOnlySpan<byte> signature) =>
        contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => IsJpeg(signature),
            "image/png" => IsPng(signature),
            "image/webp" => IsWebp(signature),
            _ => false
        };

    private static bool MatchesVideoDeclaredType(string contentType, ReadOnlySpan<byte> signature) =>
        contentType.ToLowerInvariant() switch
        {
            "video/mp4" => IsMp4OrMov(signature),
            "video/quicktime" => IsMp4OrMov(signature), // MOV file
            _ => false
        };

    private static bool IsJpeg(ReadOnlySpan<byte> signature) =>
        signature.Length >= 3 && signature[0] == 0xFF && signature[1] == 0xD8 && signature[2] == 0xFF;

    private static bool IsPng(ReadOnlySpan<byte> signature) =>
        signature.Length >= 8 && signature[..8].SequenceEqual(PngSignature);

    private static bool IsWebp(ReadOnlySpan<byte> signature) =>
        signature.Length >= 12 && signature[..4].SequenceEqual("RIFF"u8) && signature[8..12].SequenceEqual("WEBP"u8);

    // Kỹ thuật soi Magic Bytes cho MP4 và MOV: Byte từ 4 đến 7 phải là 'ftyp'
    private static bool IsMp4OrMov(ReadOnlySpan<byte> signature) =>
        signature.Length >= 8 && signature[4..8].SequenceEqual("ftyp"u8);
}