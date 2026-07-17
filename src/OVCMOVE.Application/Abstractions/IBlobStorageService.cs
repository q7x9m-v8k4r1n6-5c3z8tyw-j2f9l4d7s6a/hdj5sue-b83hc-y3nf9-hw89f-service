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
}