using DotNetEnv;

using OVCMOVE.Api.Extensions;
using OVCMOVE.Api.Middleware;
using OVCMOVE.Api.Services;
using OVCMOVE.API.Hubs;
using OVCMOVE.Application;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Local"))
{
    var apiEnvPath = Path.Combine(
        Directory.GetCurrentDirectory(), "src", "OVCMOVE.Api", ".env");
    var envFiles = File.Exists(apiEnvPath)
        ? Env.Load(apiEnvPath)
        : Env.TraversePath().Load();

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

builder.Services.AddMemoryCache();
builder.Services.AddAppRateLimit(builder.Configuration);

// Register the core MVC services first so an optional plugin can safely add
// its application part without being overwritten by a later AddControllers call.
builder.Services.AddApiControllers();
OptionalPluginLoader.RegisterMove2026(builder.Services, builder.Configuration);
builder.Services.AddSwaggerDocumentation();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActorProvider, HttpContextCurrentActorProvider>();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRbacAuthorization();
builder.Services.AddCustomCors(builder.Configuration);

builder.Services.AddSignalR();
builder.Services.AddScoped<IBoothNotificationService, BoothNotificationService>();

var app = builder.Build();

app.UseSwaggerDocumentation();
app.UseCors("AllowFrontend");
app.UseHttpsRedirection();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<BoothHub>("/api/v1/hubs/booth");

app.Run();
