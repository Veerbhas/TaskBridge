using TaskBridge.Api.Notifications.Models;

namespace TaskBridge.Api.Notifications.Repositories;

public interface INotificationRepository
{
    void AddRange(IEnumerable<Notification> notifications);

    Task<IReadOnlyList<Notification>> GetUnreadForUserAsync(
        string userId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Notification?> FindForUserAsync(
        Guid notificationId,
        string userId,
        Guid organizationId,
        CancellationToken cancellationToken = default);
}