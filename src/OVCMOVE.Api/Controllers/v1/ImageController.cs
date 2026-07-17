using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Application.Features.Images.Command;
using OVCMOVE.Application.Features.Images.Command.UploadImage;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Api.Controllers.v1;

public class ImageController : BaseController<ImageController>
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // Giới hạn 5MB
    private static readonly string[] AllowedContentTypes =
    {
        "image/jpeg", "image/png", "image/webp"
    };

    public ImageController(ILogger<ImageController> logger, IMediator mediator, IMapper mapper)
        : base(logger, mediator, mapper)
    {
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return Ok(new InternalServerErrorModel("Vui lòng chọn 1 file ảnh để upload."));
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return Ok(new InternalServerErrorModel("File ảnh vượt quá giới hạn 5MB."));
            }

            if (!AllowedContentTypes.Contains(file.ContentType))
            {
                return Ok(new InternalServerErrorModel("Chỉ chấp nhận file ảnh định dạng JPG, PNG, hoặc WEBP."));
            }

            using var stream = file.OpenReadStream();
            var command = new UploadImageCommand(stream, file.FileName, file.ContentType);
            var url = await _mediator.Send(command, cancellationToken);

            return Ok(new ApiResponseModel<string?>
            {
                StatusCode = APIContansts.StatusCode.Success,
                Message = APIContansts.StatusMessage.Success,
                Data = url
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while processing UploadImage: {Message}", ex.Message);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }
}