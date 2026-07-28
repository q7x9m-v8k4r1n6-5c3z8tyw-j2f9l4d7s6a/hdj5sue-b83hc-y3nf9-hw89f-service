namespace OVCMOVE.Application.Abstractions;

public interface IBlobStorageService
{
    /// <summary>
    /// Upload 1 file lên blob storage, trả về URL công khai của file đó.
    /// </summary>
    Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Best-effort removal used to compensate for a failed database write.
    /// Returns false instead of masking the original failure.
    /// </summary>
    Task<bool> TryDeleteAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);
}
