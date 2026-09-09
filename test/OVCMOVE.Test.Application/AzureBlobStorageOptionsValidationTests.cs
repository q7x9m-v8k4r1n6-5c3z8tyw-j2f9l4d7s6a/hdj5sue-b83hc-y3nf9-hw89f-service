using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OVCMOVE.Infrastructure;
using OVCMOVE.Infrastructure.Options;

namespace OVCMOVE.Test.Application;

public class AzureBlobStorageOptionsValidationTests
{
    [Fact]
    public void StartupValidation_ThrowsOptionsValidationException_WhenMapContainerNameIsMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DbConfig:SQLServer:ConnectionString"] = "Server=localhost;Database=test;",
                ["JwtConfig:SecretKey"] = "SecretKeySecretKeySecretKeySecretKey12345",
                ["JwtConfig:SigningKeyId"] = "key-01",
                ["JwtConfig:Issuer"] = "test-issuer",
                ["JwtConfig:Audience"] = "test-audience",
                ["GoogleAuthConfig:ClientId"] = "google-client-id",
                ["AzureBlobStorage:ConnectionString"] = "UseDevelopmentStorage=true;",
                ["AzureBlobStorage:ContainerName"] = "race-images",
                // MapContainerName is omitted or whitespace
                ["AzureBlobStorage:MapContainerName"] = ""
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);

        var serviceProvider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() =>
        {
            _ = serviceProvider.GetRequiredService<IOptions<AzureBlobStorageOptions>>().Value;
        });

        Assert.Contains("map container name are required", exception.Message);
    }

    [Fact]
    public void StartupValidation_Succeeds_WhenAllAzureBlobStorageOptionsProvided()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DbConfig:SQLServer:ConnectionString"] = "Server=localhost;Database=test;",
                ["JwtConfig:SecretKey"] = "SecretKeySecretKeySecretKeySecretKey12345",
                ["JwtConfig:SigningKeyId"] = "key-01",
                ["JwtConfig:Issuer"] = "test-issuer",
                ["JwtConfig:Audience"] = "test-audience",
                ["GoogleAuthConfig:ClientId"] = "google-client-id",
                ["AzureBlobStorage:ConnectionString"] = "UseDevelopmentStorage=true;",
                ["AzureBlobStorage:ContainerName"] = "race-images",
                ["AzureBlobStorage:MapContainerName"] = "race-map"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);

        var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<IOptions<AzureBlobStorageOptions>>().Value;

        Assert.Equal("race-map", options.MapContainerName);
        Assert.Equal("race-images", options.ContainerName);
    }
}
