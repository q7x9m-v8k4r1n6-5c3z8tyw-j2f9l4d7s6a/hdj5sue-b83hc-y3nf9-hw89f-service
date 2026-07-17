using DotNetEnv;

using OVCMOVE.Api.Extensions;
using OVCMOVE.Api.Middleware;
using OVCMOVE.Application;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Infrastructure;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Local"))
{
    // TraversePath() sẽ tự động quét file .env từ thư mục hiện tại hất ngược lên các thư mục cha
    // Vì vậy dù bạn để .env ở thư mục Api, hay thư mục sln ngoài cùng, nó đều tìm thấy.
    var envFiles = Env.TraversePath().Load();

    if (envFiles.Any())
    {
        Console.WriteLine("✅ THÀNH CÔNG: Đã nạp biến môi trường từ file .env");
    }
    else
    {
        Console.WriteLine("⚠️ CẢNH BÁO: KHÔNG TÌM THẤY file .env nào! Hệ thống sẽ dùng cấu hình mặc định.");
    }
}

builder.Configuration
    .AddEnvironmentVariables()
    .AddEnvironmentVariables(prefix: "OVCMOVE_");

builder.Services.AddMapping();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration); 
builder.Services.AddControllers();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCustomCors();
    
var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwaggerDocumentation();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();