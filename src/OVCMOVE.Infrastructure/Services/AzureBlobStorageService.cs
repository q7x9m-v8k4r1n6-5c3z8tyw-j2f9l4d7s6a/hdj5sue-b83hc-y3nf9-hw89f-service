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
        var blobServiceClient = new BlobServiceClient(configuration.ConnectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(configuration.ContainerName);
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var response = await _containerClient.CreateIfNotExistsAsync(
            PublicAccessType.Blob,
            cancellationToken: cancellationToken);

        if (response == null)
        {
            await _containerClient.SetAccessPolicyAsync(
                PublicAccessType.Blob,
                cancellationToken: cancellationToken);
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var blobClient = _containerClient.GetBlobClient(uniqueFileName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        await blobClient.UploadAsync(fileStream, uploadOptions, cancellationToken);

        return blobClient.Uri.ToString();
    }

    /// <inheritdoc />
    public async Task<bool> TryDeleteAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var fileUri))
            {
                return false;
            }

            var containerPrefix = $"{_containerClient.Uri.AbsoluteUri.TrimEnd('/')}/";
            if (!fileUri.AbsoluteUri.StartsWith(containerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Skipped blob deletion outside configured container: {FileUrl}.", fileUrl);
                return false;
            }

            var relativePath = _containerClient.Uri.MakeRelativeUri(fileUri).ToString();
            var fileName = Uri.UnescapeDataString(relativePath);

            var deleteResponse = await _containerClient
                .GetBlobClient(fileName)
                .DeleteIfExistsAsync(cancellationToken: cancellationToken);

            return deleteResponse.Value;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not remove orphaned blob {FileUrl}.", fileUrl);
            return false;
        }
    }
}