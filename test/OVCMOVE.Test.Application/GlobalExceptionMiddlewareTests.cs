using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Middleware;

namespace OVCMOVE.Test.Application;

public sealed class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_InvalidJsonPayload_ReturnsBadRequestEnvelope()
    {
        var context = new DefaultHttpContext();
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        var middleware = new GlobalExceptionMiddleware(
            _ =>
            {
                JsonPayloadDeserializer.Deserialize<PayloadModel>("{");
                return Task.CompletedTask;
            },
            NullLogger<GlobalExceptionMiddleware>.Instance,
            new TestHostEnvironment());

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        responseBody.Position = 0;
        var response = await JsonSerializer.DeserializeAsync<
            ApiResponseModel<object>>(
            responseBody,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(response);
        Assert.Equal(ApiStatus.Codes.BadRequest, response.StatusCode);
        Assert.Equal(ApiStatus.Messages.BadRequest, response.Message);
        Assert.Equal("Payload JSON không hợp lệ.", response.DetailError);
    }

    private sealed class PayloadModel;

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "OVCMOVE.Test.Application";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}
