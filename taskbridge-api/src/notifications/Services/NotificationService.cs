using Microsoft.Extensions.Logging;
using TaskBridge.Api.Notifications.Models;
using TaskBridge.Api.Notifications.Repositories;
using TaskBridge.Api.Projects.Models;
using TaskBridge.Api.Projects.Repositories;
using TaskBridge.Api.Projects.Services;

namespace TaskBridge.Api.Notifications.Services;

public sealed class NotificationService(
    INotificationRepository notificationRepository,
    ITeamMemberRepository teamMemberRepository,
    IUnitOfWork unitOfWork,
    ICurrentActorContext currentActor,
    TimeProvider timeProvider,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task<int> QueueProjectEventAsync(
        Project project,
        string eventType,
        string message,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        if (project.OrganizationId != currentActor.OrganizationId)
        {
            throw new UnauthorizedAccessException("Cannot notify users outside the current organization.");
        }

        var recipientIds = await teamMemberRepository.GetUserIdsByTeamAsync(
            project.TeamId,
            project.OrganizationId,
            cancellationToken);

        if (recipientIds.Count == 0)
        {
            logger.LogWarning(
                "No team members found for project {ProjectId} in organization {OrganizationId}",
                project.Id,
                project.OrganizationId);
            return 0;
        }

        var notifications = recipientIds
            .Distinct(StringComparer.Ordinal)
            .Select(userId => new Notification(
                userId,
                project.Id,
                eventType,
                message,
                project.OrganizationId,
                createdAt))
            .ToArray();

        notificationRepository.AddRange(notifications);
        logger.LogInformation(
            "Queued {NotificationCount} {EventType} notifications for project {ProjectId} in organization {OrganizationId}",
            notifications.Length,
            eventType,
            project.Id,
            project.OrganizationId);

        return notifications.Length;
    }

    public Task<IReadOnlyList<Notification>> GetUnreadForCurrentUserAsync(
        CancellationToken cancellationToken = default) =>
        notificationRepository.GetUnreadForUserAsync(
            currentActor.UserId,
            currentActor.OrganizationId,
            cancellationToken);

    public async Task<bool> MarkReadForCurrentUserAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        if (notificationId == Guid.Empty)
        {
            throw new ArgumentException("Notification ID cannot be empty.", nameof(notificationId));
        }

        var notification = await notificationRepository.FindForUserAsync(
            notificationId,
            currentActor.UserId,
            currentActor.OrganizationId,
            cancellationToken);
        if (notification is null)
        {
            return false;
        }

        notification.MarkAsRead(timeProvider.GetUtcNow());
        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }
}
