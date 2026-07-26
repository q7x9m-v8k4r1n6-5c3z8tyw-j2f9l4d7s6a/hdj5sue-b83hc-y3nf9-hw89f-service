using Microsoft.Extensions.DependencyInjection;
using OVCMOVE.Application.Behaviors;

namespace OVCMOVE.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            configuration.AddOpenBehavior(typeof(AuditFieldsBehavior<,>));
        });

        services.AddAutoMapper(AssemblyReference.Assembly);

        return services;
    }
}
