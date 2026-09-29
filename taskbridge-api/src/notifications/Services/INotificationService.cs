using TaskBridge.Api.Notifications.Models;
using TaskBridge.Api.Projects.Models;

namespace TaskBridge.Api.Notifications.Services;

public interface INotificationService
{
    Task<int> QueueProjectEventAsync(
        Project project,
        string eventType,
        string message,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetUnreadForCurrentUserAsync(
        CancellationToken cancellationToken = default);

    Task<bool> MarkReadForCurrentUserAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);
}