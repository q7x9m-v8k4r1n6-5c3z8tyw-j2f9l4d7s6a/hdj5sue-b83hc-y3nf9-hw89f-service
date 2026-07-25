using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Teams.Command.CreateTeam;

namespace OVCMOVE.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddAutoMapper(AssemblyReference.Assembly);
        services.AddScoped<IValidator<CreateTeamCommand>, CreateTeamCommandValidator>();

        return services;
    }
}
