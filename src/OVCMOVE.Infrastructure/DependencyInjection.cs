using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Options;
using OVCMOVE.Infrastructure.Persistance.SqlServer;
using OVCMOVE.Infrastructure.Repositories;

namespace OVCMOVE.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        #region =================== Options ====================
        services.Configure<DbConfigOptions>(
            configuration.GetSection(DbConfigOptions.SectionName));
        
        services.Configure<ExternalServicesConfigOptions>(
            configuration.GetSection(ExternalServicesConfigOptions.SectionName));

        services.Configure<JwtConfigOptions>(
            configuration.GetSection(JwtConfigOptions.SectionName));

        #endregion

        services.AddSingleton<ISqlServerFactory, SqlServerFactory>();
        services.AddScoped<IDapperHelper, DapperHelper>();

        #region ==================== Repositories ====================
        services.AddScoped<IExampleRepository, ExampleRepository>();
        
        #endregion

        return services;
    }
}
