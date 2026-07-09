using DotNetEnv;
using OVCMOVE.Api.Extensions;
using OVCMOVE.Application;
using OVCMOVE.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsEnvironment("Local"))
{
    var envFilePath = FindEnvFile(builder.Environment.ContentRootPath);
    if (envFilePath is not null)
    {
        Env.Load(envFilePath);
    }
}

static string? FindEnvFile(string startPath)
{
    var directory = new DirectoryInfo(startPath);

    while (directory is not null)
    {
        var envFilePath = Path.Combine(directory.FullName, ".env");
        if (File.Exists(envFilePath))
        {
            return envFilePath;
        }

        directory = directory.Parent;
    }

    return null;
}

builder.Configuration
    .AddEnvironmentVariables()
    .AddEnvironmentVariables(prefix: "OVCMOVE_");

builder.Services.AddMapping();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

app.UseSwaggerDocumentation();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
