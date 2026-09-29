using Microsoft.EntityFrameworkCore;
using TaskBridge.Api.Data;
using TaskBridge.Api.Notifications.Models;

namespace TaskBridge.Api.Notifications.Repositories;

public sealed class NotificationRepository(TaskBridgeDbContext dbContext) : INotificationRepository
{
    public void AddRange(IEnumerable<Notification> notifications) => dbContext.Notifications.AddRange(notifications);

    public async Task<IReadOnlyList<Notification>> GetUnreadForUserAsync(
        string userId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Notifications
            .AsNoTracking()
            .Where(notification =>
                notification.RecipientUserId == userId &&
                notification.OrganizationId == organizationId &&
                !notification.IsRead)
            .OrderByDescending(notification => notification.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<Notification?> FindForUserAsync(
        Guid notificationId,
        string userId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Notifications.FirstOrDefaultAsync(notification =>
            notification.Id == notificationId &&
            notification.RecipientUserId == userId &&
            notification.OrganizationId == organizationId,
            cancellationToken);
}