namespace TaskBridge.Api.Notifications.Models;

public sealed class Notification
{
    private Notification()
    {
        RecipientUserId = string.Empty;
        EventType = string.Empty;
        Message = string.Empty;
    }

    public Notification(
        string recipientUserId,
        Guid projectId,
        string eventType,
        string message,
        Guid organizationId,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (projectId == Guid.Empty || organizationId == Guid.Empty)
        {
            throw new ArgumentException("Project and organization IDs must be non-empty.");
        }

        if (recipientUserId.Length > 200 || eventType.Length > 100 || message.Length > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(message), "Notification field length exceeds its limit.");
        }

        Id = Guid.NewGuid();
        RecipientUserId = recipientUserId;
        ProjectId = projectId;
        EventType = eventType;
        Message = message;
        OrganizationId = organizationId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string RecipientUserId { get; private set; }

    public Guid ProjectId { get; private set; }

    public string EventType { get; private set; }

    public string Message { get; private set; }

    public bool IsRead { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public Guid OrganizationId { get; private set; }

    public void MarkAsRead(DateTimeOffset readAt)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAt = readAt;
    }
}
