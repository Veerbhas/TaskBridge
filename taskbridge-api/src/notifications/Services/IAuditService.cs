using TaskBridge.Api.Notifications.Models;
using TaskBridge.Api.Projects.Models;

namespace TaskBridge.Api.Notifications.Services;

public interface IAuditService
{
    Task<AuditEntry?> RecordInternalEventAsync(
        Guid projectId,
        string eventType,
        string? previousState,
        string? newState,
        CancellationToken cancellationToken = default);

    void RecordProjectEvent(
        Project project,
        string eventType,
        string? previousState,
        string? newState,
        DateTimeOffset timestamp);

    Task<IReadOnlyList<AuditEntry>> GetProjectHistoryAsync(
        Guid projectId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? eventType,
        CancellationToken cancellationToken = default);
}