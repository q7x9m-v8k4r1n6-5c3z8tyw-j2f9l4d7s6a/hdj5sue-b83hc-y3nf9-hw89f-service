using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Infrastructure.Options;

namespace OVCMOVE.Infrastructure.Services;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        IOptions<AzureBlobStorageOptions> options,
        ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        var configuration = options.Value;
        var blobServiceClient = new BlobServiceClient(
            configuration.ConnectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(
            configuration.ContainerName);
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await _containerClient.CreateIfNotExistsAsync(
            PublicAccessType.Blob,
            cancellationToken: cancellationToken);
        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var blobClient = _containerClient.GetBlobClient(uniqueFileName);
        var blobHttpHeaders = new BlobHttpHeaders
        {
            ContentType = contentType
        };

        await blobClient.UploadAsync(
            fileStream,
            new BlobUploadOptions { HttpHeaders = blobHttpHeaders },
            cancellationToken);

        return blobClient.Uri.ToString();
    }

    /// <inheritdoc />
    public async Task<bool> TryDeleteAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileUri = new Uri(fileUrl, UriKind.Absolute);
            var containerPrefix =
                $"{_containerClient.Uri.AbsoluteUri.TrimEnd('/')}/";
            if (!fileUri.AbsoluteUri.StartsWith(
                containerPrefix,
                StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Skipped blob deletion outside configured container: {FileUrl}.",
                    fileUrl);
                return false;
            }

            var fileName = Uri.UnescapeDataString(
                fileUri.Segments[^1]);
            var response = await _containerClient
                .GetBlobClient(fileName)
                .DeleteIfExistsAsync(cancellationToken: cancellationToken);
            return response.Value;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Could not remove orphaned blob {FileUrl}.",
                fileUrl);
            return false;
        }
    }
}
