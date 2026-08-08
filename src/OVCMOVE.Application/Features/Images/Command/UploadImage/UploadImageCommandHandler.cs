using MediatR;
using OVCMOVE.Application.Abstractions;

namespace OVCMOVE.Application.Features.Images.Command.UploadImage;

public class UploadImageCommandHandler :
    IRequestHandler<UploadImageCommand, string>
{
    private readonly IBlobStorageService _blobStorageService;

    public UploadImageCommandHandler(
        IBlobStorageService blobStorageService)
    {
        _blobStorageService = blobStorageService;
    }

    /// <summary>Uploads one already-validated image to blob storage.</summary>
    public Task<string> Handle(
        UploadImageCommand request,
        CancellationToken cancellationToken) =>
        _blobStorageService.UploadAsync(
            request.File.Stream,
            request.File.FileName,
            request.File.ContentType,
            cancellationToken: cancellationToken);
}
