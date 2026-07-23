using Microsoft.Extensions.DependencyInjection;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Services;

namespace OVCMOVE.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(AssemblyReference.Assembly);
        });

        services.AddAutoMapper(AssemblyReference.Assembly);
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
