using DotNetEnv;
using OVCMOVE.Api.Extensions;
using OVCMOVE.Application;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Infrastructure;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsEnvironment("Local"))
{
    const string envFilePath = "./.env";
    if (File.Exists(envFilePath))
    {
        Env.Load(envFilePath);
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

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();

    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

var app = builder.Build();

app.UseSwaggerDocumentation();

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();