using TaskBridge.Api.Notifications.Models;

namespace TaskBridge.Api.Notifications.Repositories;

public interface IAuditRepository
{
    void Add(AuditEntry auditEntry);

    Task<IReadOnlyList<AuditEntry>> GetProjectHistoryAsync(
        Guid projectId,
        Guid organizationId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? eventType,
        CancellationToken cancellationToken = default);
}
