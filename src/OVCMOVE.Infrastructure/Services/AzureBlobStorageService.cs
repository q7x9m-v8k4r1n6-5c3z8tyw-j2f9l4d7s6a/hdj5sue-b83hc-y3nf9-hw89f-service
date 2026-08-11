using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Infrastructure.Options;

namespace OVCMOVE.Infrastructure.Services;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _defaultContainerName;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        IOptions<AzureBlobStorageOptions> options,
        ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        var configuration = options.Value;
        
        _blobServiceClient = new BlobServiceClient(configuration.ConnectionString);
        
        _defaultContainerName = configuration.ContainerName;
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? containerName = null, 
        CancellationToken cancellationToken = default)
    {
        var targetContainer = string.IsNullOrWhiteSpace(containerName) ? _defaultContainerName : containerName;
        var containerClient = _blobServiceClient.GetBlobContainerClient(targetContainer);

        await containerClient.CreateIfNotExistsAsync(
            PublicAccessType.Blob,
            cancellationToken: cancellationToken);
            
        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var blobClient = containerClient.GetBlobClient(uniqueFileName);
        var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };

        await blobClient.UploadAsync(
            fileStream,
            new BlobUploadOptions { HttpHeaders = blobHttpHeaders },
            cancellationToken);

        return blobClient.Uri.ToString();
    }

    public async Task<bool> TryDeleteAsync(
        string fileUrl,
        string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var targetContainer = string.IsNullOrWhiteSpace(containerName) ? _defaultContainerName : containerName;
            var containerClient = _blobServiceClient.GetBlobContainerClient(targetContainer);

            var fileUri = new Uri(fileUrl, UriKind.Absolute);
            var containerPrefix = $"{containerClient.Uri.AbsoluteUri.TrimEnd('/')}/";
            if (!fileUri.AbsoluteUri.StartsWith(containerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Skipped blob deletion outside configured container: {FileUrl}.", fileUrl);
                return false;
            }

            var fileName = Uri.UnescapeDataString(fileUri.Segments[^1]);
            var response = await containerClient.GetBlobClient(fileName).DeleteIfExistsAsync(cancellationToken: cancellationToken);
            return response.Value;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not remove orphaned blob {FileUrl}.", fileUrl);
            return false;
        }
    }
}