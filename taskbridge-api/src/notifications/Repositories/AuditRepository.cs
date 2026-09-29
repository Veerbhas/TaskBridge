using TaskBridge.Api.Data;
using TaskBridge.Api.Notifications.Models;
using Microsoft.EntityFrameworkCore;

namespace TaskBridge.Api.Notifications.Repositories;

public sealed class AuditRepository(TaskBridgeDbContext dbContext) : IAuditRepository
{
    public void Add(AuditEntry auditEntry) => dbContext.AuditEntries.Add(auditEntry);

    public async Task<IReadOnlyList<AuditEntry>> GetProjectHistoryAsync(
        Guid projectId,
        Guid organizationId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? eventType,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.AuditEntries
            .AsNoTracking()
            .Where(entry => entry.ProjectId == projectId && entry.OrganizationId == organizationId);

        if (from.HasValue)
        {
            query = query.Where(entry => entry.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(entry => entry.Timestamp < to.Value);
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(entry => entry.EventType == eventType);
        }

        return await query
            .OrderByDescending(entry => entry.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
