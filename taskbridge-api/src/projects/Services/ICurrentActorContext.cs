namespace TaskBridge.Api.Projects.Services;

public interface ICurrentActorContext
{
    Guid OrganizationId { get; }

    string UserId { get; }

    string? IpAddress { get; }
}