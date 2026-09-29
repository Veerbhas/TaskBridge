using TaskBridge.Api.Notifications.Models;
using TaskBridge.Api.Notifications.Repositories;
using TaskBridge.Api.Projects.Models;
using TaskBridge.Api.Projects.Repositories;
using TaskBridge.Api.Projects.Services;

namespace TaskBridge.Api.Notifications.Services;

public sealed class AuditService(
    IAuditRepository auditRepository,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork,
    ICurrentActorContext currentActor,
    TimeProvider timeProvider) : IAuditService
{
    private static readonly HashSet<string> SupportedEventTypes = new(StringComparer.Ordinal)
    {
        "PROJECT_CREATED",
        "PROJECT_UPDATED",
        "PROJECT_DELETED",
        "MILESTONE_CREATED",
        "MILESTONE_UPDATED",
        "MILESTONE_CLOSED",
        "MILESTONE_REOPENED"
    };

    public async Task<AuditEntry?> RecordInternalEventAsync(
        Guid projectId,
        string eventType,
        string? previousState,
        string? newState,
        CancellationToken cancellationToken = default)
    {
        ValidateEventType(eventType);
        var project = await projectRepository.FindByIdAsync(
            projectId,
            currentActor.OrganizationId,
            cancellationToken);
        if (project is null)
        {
            return null;
        }

        var entry = new AuditEntry(
            project.Id,
            eventType,
            currentActor.UserId,
            project.OrganizationId,
            previousState,
            newState,
            timeProvider.GetUtcNow(),
            currentActor.IpAddress);
        auditRepository.Add(entry);
        await unitOfWork.CommitAsync(cancellationToken);
        return entry;
    }

    public void RecordProjectEvent(
        Project project,
        string eventType,
        string? previousState,
        string? newState,
        DateTimeOffset timestamp)
    {
        ValidateEventType(eventType);
        if (project.OrganizationId != currentActor.OrganizationId)
        {
            throw new UnauthorizedAccessException("Cannot record an audit event outside the current organization.");
        }

        auditRepository.Add(new AuditEntry(
            project.Id,
            eventType,
            currentActor.UserId,
            project.OrganizationId,
            previousState,
            newState,
            timestamp,
            currentActor.IpAddress));
    }

    public Task<IReadOnlyList<AuditEntry>> GetProjectHistoryAsync(
        Guid projectId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? eventType,
        CancellationToken cancellationToken = default)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project ID cannot be empty.", nameof(projectId));
        }

        if (from.HasValue && to.HasValue && from.Value >= to.Value)
        {
            throw new ArgumentException("The from timestamp must be earlier than the to timestamp.");
        }

        return auditRepository.GetProjectHistoryAsync(
            projectId,
            currentActor.OrganizationId,
            from,
            to,
            string.IsNullOrWhiteSpace(eventType) ? null : eventType.Trim(),
            cancellationToken);
    }

    public static bool IsSupportedEventType(string? eventType) =>
        !string.IsNullOrWhiteSpace(eventType) && SupportedEventTypes.Contains(eventType);

    private static void ValidateEventType(string eventType)
    {
        if (!IsSupportedEventType(eventType))
        {
            throw new ArgumentException("The audit event type is not supported.", nameof(eventType));
        }
    }
}
