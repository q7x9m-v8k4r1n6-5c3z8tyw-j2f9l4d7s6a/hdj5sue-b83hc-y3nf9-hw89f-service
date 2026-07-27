using Microsoft.Extensions.DependencyInjection;
using OVCMOVE.Application.Behaviors;
using OVCMOVE.Application.Features.Auth;
using OVCMOVE.Application.Features.Races.Command.CreateRace;
using OVCMOVE.Application.Features.Races.Command.PatchRace;

namespace OVCMOVE.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            configuration.AddOpenBehavior(typeof(AuditActorBehavior<,>));
        });

        services.AddScoped<BoothPatchProcessor>();
        services.AddScoped<RaceTeamPatchProcessor>();
        services.AddScoped<RaceOrganizerPatchProcessor>();
        services.AddScoped<CreateRaceRelationValidator>();
        services.AddScoped<AuthSessionIssuer>();

        return services;
    }
}
