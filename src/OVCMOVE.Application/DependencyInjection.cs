using Microsoft.Extensions.DependencyInjection;
using OVCMOVE.Application.Behaviors;
using OVCMOVE.Application.Features.Auth;
using OVCMOVE.Application.Features.Races.Command.CreateRace;
using OVCMOVE.Application.Features.Races.Command.PatchRace;
using OVCMOVE.Application.Features.Workflows.Command;
using OVCMOVE.Application.Features.Workflows.Common;
using OVCMOVE.Application.Abstractions.Services;

namespace OVCMOVE.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(AssemblyReference.Assembly);

            // Đăng ký Pipeline Behavior để tự động ghi log Audit người dùng
            configuration.AddOpenBehavior(typeof(AuditActorBehavior<,>));
        });

        // Đăng ký các Processor xử lý nghiệp vụ Patch & Security Session
        services.AddScoped<BoothPatchProcessor>();
        services.AddScoped<RaceTeamPatchProcessor>();
        services.AddScoped<RaceOrganizerPatchProcessor>();
        services.AddScoped<CreateRaceRelationValidator>();
        services.AddScoped<AuthSessionIssuer>();
        services.AddScoped<WorkflowDefinitionValidator>();
        services.AddScoped<WorkflowRuntime>();
        services.AddScoped<WorkflowRetryPolicy>();
        services.AddScoped<WorkflowRealtimeBuffer>();
        services.AddScoped<WorkflowRealtimePublisher>();
        services.AddScoped<ICardWorkflowDispatcher, CardWorkflowDispatcher>();

        return services;
    }
}
