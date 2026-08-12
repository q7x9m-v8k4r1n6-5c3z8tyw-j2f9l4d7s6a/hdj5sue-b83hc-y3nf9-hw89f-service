using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Text.Json;

namespace OVCMOVE.Api.Controllers.v1;

[ApiController]
[ApiExplorerSettings(GroupName = "v1")]
[EnableRateLimiting("InternalApiPolicy")]
public abstract class BaseController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    protected readonly IMediator _mediator;

    protected BaseController(IMediator mediator)
    {
        _mediator = mediator;
    }

    protected Guid GetRequiredCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Token không hợp lệ.");
    }

    protected static T DeserializePayload<T>(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Payload không được để trống.");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(payload, JsonOptions)
                ?? throw new ArgumentException("Payload không hợp lệ.");
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "Payload JSON không hợp lệ.",
                exception);
        }
    }
}
