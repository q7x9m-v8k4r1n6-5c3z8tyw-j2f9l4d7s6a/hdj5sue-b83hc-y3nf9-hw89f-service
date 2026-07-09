using Microsoft.Extensions.DependencyInjection;
using OVCMOVE.Application.Mapping;

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
        return services;
    }
}

