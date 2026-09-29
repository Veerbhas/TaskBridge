namespace TaskBridge.Api.Notifications.Models;

public sealed class AuditEntry
{
    private AuditEntry()
    {
        EntityType = string.Empty;
        EventType = string.Empty;
        ActorUserId = string.Empty;
    }

    public AuditEntry(
        Guid projectId,
        string eventType,
        string actorUserId,
        Guid organizationId,
        string? previousState,
        string? newState,
        DateTimeOffset timestamp,
        string? actorIpAddress)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        EntityType = "Project";
        EventType = eventType;
        ActorUserId = actorUserId;
        OrganizationId = organizationId;
        PreviousState = previousState;
        NewState = newState;
        Timestamp = timestamp;
        ActorIpAddress = actorIpAddress;
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public string EntityType { get; private set; }

    public string EventType { get; private set; }

    public string ActorUserId { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string? PreviousState { get; private set; }

    public string? NewState { get; private set; }

    public DateTimeOffset Timestamp { get; private set; }

    public string? ActorIpAddress { get; private set; }
}
