using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Options;
using OVCMOVE.Infrastructure.Persistance.SqlServer;
using OVCMOVE.Infrastructure.Repositories;
using OVCMOVE.Infrastructure.Services;

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

        services.Configure<GoogleAuthConfigOptions>(
            configuration.GetSection(GoogleAuthConfigOptions.SectionName));
        #endregion

        services.AddSingleton<ISqlServerFactory, SqlServerFactory>();
        services.AddScoped<IDapperHelper, DapperHelper>();

        #region ==================== Repositories ====================
        services.AddScoped<IExampleRepository, ExampleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRaceRepository, RaceRepository>();
        services.AddScoped<IBoothRepository, BoothRepository>();
        services.AddScoped<IRaceTeamRepository, RaceTeamRepository>();
        services.AddScoped<IRaceOrganizerRepository, RaceOrganizerRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IOrganizerRepository, OrganizerRepository>();
        #endregion

        #region ==================== Services ====================
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IEmailService, EmailService>();
        #endregion

        #region ==================== BackgroundJobs ====================
        services.AddHostedService<BackgroundJobs.CleanupOldTokenService>();
        #endregion

        return services;
    }
}
