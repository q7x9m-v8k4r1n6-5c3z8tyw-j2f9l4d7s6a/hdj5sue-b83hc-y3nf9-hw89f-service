using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;

namespace OVCMOVE.Api.Extensions;

public static class ControllerExtensions
{
    /// <summary>Adds controllers with the same error envelope for model-binding failures.</summary>
    public static IServiceCollection AddApiControllers(
        this IServiceCollection services)
    {
        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var details = context.ModelState.Values
                        .SelectMany(entry => entry.Errors)
                        .Select(error => string.IsNullOrWhiteSpace(
                            error.ErrorMessage)
                            ? "Payload không hợp lệ."
                            : error.ErrorMessage)
                        .Distinct()
                        .ToArray();

                    return new BadRequestObjectResult(ApiResponse.Error(
                        ApiStatus.Codes.BadRequest,
                        ApiStatus.Messages.BadRequest,
                        string.Join(" ", details)));
                };
            });

        return services;
    }
}
