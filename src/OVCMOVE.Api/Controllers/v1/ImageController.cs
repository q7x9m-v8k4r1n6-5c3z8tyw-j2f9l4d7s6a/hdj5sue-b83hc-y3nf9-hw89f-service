using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Security;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Images.Command.UploadImage;

namespace OVCMOVE.Api.Controllers.v1;

[Route("api/v1/[controller]")]
public class ImageController : BaseController
{
    public ImageController(IMediator mediator) : base(mediator)
    {
    }
    // [RequirePermission(PermissionCodes.ImageUpload)]

    [HttpPost("upload")]
    [AllowAnonymous]
    public async Task<IActionResult> Upload(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        var validationError = await ImageFileValidator.ValidateAsync(
            file,
            cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(ApiResponse.Error(
                ApiStatus.Codes.BadRequest,
                ApiStatus.Messages.BadRequest,
                validationError));
        }

        using var stream = file!.OpenReadStream();
        var url = await _mediator.Send(
            new UploadImageCommand(new FileUploadModel(
                stream,
                file.FileName,
                file.ContentType)),
            cancellationToken);
        return Ok(ApiResponse.Success(url));
    }
}
