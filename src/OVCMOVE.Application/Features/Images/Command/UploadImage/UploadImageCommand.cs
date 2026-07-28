using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Images.Command.UploadImage;

public sealed record UploadImageCommand(
    FileUploadModel File) : IRequest<string>;
