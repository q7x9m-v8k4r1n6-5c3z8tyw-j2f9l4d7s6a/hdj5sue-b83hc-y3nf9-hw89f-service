using DotNetEnv;

using OVCMOVE.Api.Extensions;
using OVCMOVE.Api.Middleware;
using OVCMOVE.Api.Services;
using OVCMOVE.Application;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Infrastructure;
using OVCMOVE.Move2026.Plugin;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Local"))
{
    var envFiles = Env.TraversePath().Load();

    if (envFiles.Any())
    {
        Console.WriteLine("THÀNH CÔNG: Đã nạp biến môi trường từ file .env");
    }
    else
    {
        Console.WriteLine("CẢNH BÁO: KHÔNG TÌM THẤY file .env nào! Hệ thống sẽ dùng cấu hình mặc định.");
    }
}

builder.Configuration
    .AddEnvironmentVariables()
    .AddEnvironmentVariables(prefix: "OVCMOVE_");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMove2026Plugin();
builder.Services.AddApiControllers();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActorProvider, HttpContextCurrentActorProvider>();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRbacAuthorization();
builder.Services.AddCustomCors(builder.Configuration);

var app = builder.Build();

app.UseSwaggerDocumentation();

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
