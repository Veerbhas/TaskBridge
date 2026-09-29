using Microsoft.Extensions.DependencyInjection;
using TaskBridge.Api.Data;
using TaskBridge.Api.Notifications.Repositories;
using TaskBridge.Api.Notifications.Services;
using TaskBridge.Api.Projects.Repositories;

namespace TaskBridge.Api.Projects.Services;

public static class ProjectServiceCollectionExtensions
{
    public static IServiceCollection AddProjectFeature(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICurrentActorContext, HttpCurrentActorContext>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<TaskBridgeDbContext>());
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IProjectService, ProjectService>();

        return services;
    }
}