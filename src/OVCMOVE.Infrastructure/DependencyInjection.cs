using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Options;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.SqlServer;
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
        services.AddOptions<DbConfigOptions>()
            .Bind(configuration.GetSection(DbConfigOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(
                    options.SqlServer.ConnectionString),
                "DbConfig:SQLServer:ConnectionString is required.")
            .ValidateOnStart();

        services.Configure<ExternalServicesConfigOptions>(
            configuration.GetSection(ExternalServicesConfigOptions.SectionName));


        services.AddOptions<JwtConfigOptions>()
            .Bind(configuration.GetSection(JwtConfigOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.SecretKey) &&
                    !string.IsNullOrWhiteSpace(options.SigningKeyId) &&
                    !string.IsNullOrWhiteSpace(options.Issuer) &&
                    !string.IsNullOrWhiteSpace(options.Audience),
                "JwtConfig signing and issuer settings are required.")
            .ValidateOnStart();

        services.AddOptions<GoogleAuthConfigOptions>()
            .Bind(configuration.GetSection(
                GoogleAuthConfigOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ClientId),
                "GoogleAuthConfig:ClientId is required.")
            .ValidateOnStart();

        services.AddOptions<AzureBlobStorageOptions>()
            .Bind(configuration.GetSection(
                AzureBlobStorageOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.ConnectionString) &&
                    !string.IsNullOrWhiteSpace(options.ContainerName),
                "Azure blob connection string and container name are required.")
            .ValidateOnStart();

        services.AddOptions<LoginRateLimitConfigOptions>()
            .Bind(configuration.GetSection(LoginRateLimitConfigOptions.SectionName))
            .Validate(
                options => 
                    options.MaxFailedAttemptsBeforeWait > 0 &&
                    options.MaxFailedAttemptsBeforeBan > options.MaxFailedAttemptsBeforeWait &&
                    options.BaseWaitTimeSeconds > 0 &&
                    options.WaitTimeMultiplier >= 1,
                "LoginRateLimitConfig valid thresholds and wait times are required or in right format.")
            .ValidateOnStart();
        #endregion

        services.AddSingleton<ISqlServerFactory, SqlServerFactory>();
        services.AddScoped<UnitOfWork>();
        services.AddScoped<IUnitOfWork>(
            provider => provider.GetRequiredService<UnitOfWork>());
        services.AddScoped<IDbExecutor, DapperExecutor>();

        #region ==================== Repositories ====================
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRaceRepository, RaceRepository>();
        services.AddScoped<IBoothRepository, BoothRepository>();
        services.AddScoped<IBoothOrganizerRepository, BoothOrganizerRepository>();
        services.AddScoped<IRaceTeamRepository, RaceTeamRepository>();
        services.AddScoped<IRaceOrganizerRepository, RaceOrganizerRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IOrganizerRepository, OrganizerRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IUserAccessRepository, UserAccessRepository>();
        #endregion

        #region ==================== Services ====================
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
        services.AddSingleton<ILoginRateLimitService, LoginRateLimitService>();
        #endregion

        #region ==================== BackgroundJobs ====================
        services.AddHostedService<BackgroundJobs.CleanupOldTokenService>();
        #endregion

        return services;
    }
}
