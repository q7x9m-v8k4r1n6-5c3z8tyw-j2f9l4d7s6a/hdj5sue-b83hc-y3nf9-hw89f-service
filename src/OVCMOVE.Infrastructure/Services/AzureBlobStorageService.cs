using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions;

namespace OVCMOVE.Infrastructure.Services;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;

        var connectionString = configuration["AzureBlobStorage:ConnectionString"];
        var containerName = configuration["AzureBlobStorage:ContainerName"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("AzureBlobStorage:ConnectionString is not configured.");
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new InvalidOperationException("AzureBlobStorage:ContainerName is not configured.");
        }

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        // Đảm bảo container tồn tại, cho phép truy cập công khai ở mức Blob
        _containerClient.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = _containerClient.GetBlobClient(uniqueFileName);

            var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };

            await blobClient.UploadAsync(
                fileStream,
                new BlobUploadOptions { HttpHeaders = blobHttpHeaders },
                cancellationToken);

            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while uploading blob: {Message}", ex.Message);
            throw;
        }
    }
}