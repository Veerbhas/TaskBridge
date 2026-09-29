using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TaskBridge.Api.Notifications.Services;
using TaskBridge.Api.Projects.Models;
using TaskBridge.Api.Projects.Repositories;

namespace TaskBridge.Api.Projects.Services;

public sealed class ProjectService(
    IProjectRepository projectRepository,
    IAuditService auditService,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    ICurrentActorContext currentActor,
    TimeProvider timeProvider,
    ILogger<ProjectService> logger) : IProjectService
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<Project> CreateAsync(
        string name,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = currentActor.OrganizationId;
        if (!await projectRepository.TeamBelongsToOrganizationAsync(teamId, organizationId, cancellationToken))
        {
            throw new KeyNotFoundException("The team does not exist in the current organization.");
        }

        var now = timeProvider.GetUtcNow();
        var project = new Project(name, teamId, organizationId, now);
        projectRepository.Add(project);
        auditService.RecordProjectEvent(project, "PROJECT_CREATED", null, SerializeState(project), now);
        await notificationService.QueueProjectEventAsync(
            project,
            "PROJECT_CREATED",
            $"Project '{project.Name}' was created.",
            now,
            cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Project {ProjectId} created for organization {OrganizationId}",
            project.Id,
            project.OrganizationId);

        return project;
    }

    public async Task<Project?> UpdateStatusAsync(
        Guid projectId,
        ProjectStatus status,
        CancellationToken cancellationToken = default)
    {
        var organizationId = currentActor.OrganizationId;
        var project = await projectRepository.FindByIdAsync(projectId, organizationId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var previousStatus = project.Status;
        if (previousStatus == status)
        {
            return project;
        }

        var now = timeProvider.GetUtcNow();
        var previousState = SerializeState(project);
        project.ChangeStatus(status, now);
        auditService.RecordProjectEvent(project, "PROJECT_UPDATED", previousState, SerializeState(project), now);
        await notificationService.QueueProjectEventAsync(
            project,
            "PROJECT_UPDATED",
            $"Project '{project.Name}' status changed to {project.Status}.",
            now,
            cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Project {ProjectId} status changed from {PreviousStatus} to {Status} for organization {OrganizationId}",
            project.Id,
            previousStatus,
            project.Status,
            project.OrganizationId);

        return project;
    }

    public Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        projectRepository.FindByIdAsync(projectId, currentActor.OrganizationId, cancellationToken);

    public Task<IReadOnlyList<Project>> GetByTeamAsync(
        Guid teamId,
        CancellationToken cancellationToken = default) =>
        projectRepository.GetByTeamAsync(teamId, currentActor.OrganizationId, cancellationToken);

    public async Task<bool> DeleteAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = currentActor.OrganizationId;
        var project = await projectRepository.FindByIdAsync(projectId, organizationId, cancellationToken);
        if (project is null)
        {
            return false;
        }

        var now = timeProvider.GetUtcNow();
        auditService.RecordProjectEvent(project, "PROJECT_DELETED", SerializeState(project), null, now);
        await notificationService.QueueProjectEventAsync(
            project,
            "PROJECT_DELETED",
            $"Project '{project.Name}' was deleted.",
            now,
            cancellationToken);
        projectRepository.Remove(project);
        await unitOfWork.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Project {ProjectId} deleted for organization {OrganizationId}",
            project.Id,
            project.OrganizationId);

        return true;
    }

    private static string SerializeState(Project project) =>
        JsonSerializer.Serialize(new { project.Id, project.Name, project.Status, project.TeamId }, AuditJsonOptions);
}