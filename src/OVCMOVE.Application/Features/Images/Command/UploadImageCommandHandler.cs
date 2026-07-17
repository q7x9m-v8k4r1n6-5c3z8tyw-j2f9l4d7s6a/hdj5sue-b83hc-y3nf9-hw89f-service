using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Images.Command.UploadImage;

public class UploadImageCommandHandler :
    BaseCommandHandler<UploadImageCommandHandler>,
    IRequestHandler<UploadImageCommand, string?>
{
    private readonly IBlobStorageService _blobStorageService;

    public UploadImageCommandHandler(
        ILogger<UploadImageCommandHandler> logger,
        IMapper mapper,
        IBlobStorageService blobStorageService) : base(logger, mapper)
    {
        _blobStorageService = blobStorageService;
    }

    public async Task<string?> Handle(UploadImageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = await _blobStorageService.UploadAsync(
                request.FileStream,
                request.FileName,
                request.ContentType,
                cancellationToken);

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while uploading image: {Message}", ex.Message);
            throw;
        }
    }
}