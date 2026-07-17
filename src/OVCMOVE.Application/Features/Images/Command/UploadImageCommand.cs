using MediatR;

namespace OVCMOVE.Application.Features.Images.Command;

public class UploadImageCommand : IRequest<string?>
{
    public Stream FileStream { get; set; } = default!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;

    public UploadImageCommand(Stream fileStream, string fileName, string contentType)
    {
        FileStream = fileStream;
        FileName = fileName;
        ContentType = contentType;
    }
}