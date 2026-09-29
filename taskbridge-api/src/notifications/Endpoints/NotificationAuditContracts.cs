using FluentValidation;
using TaskBridge.Api.Notifications.Models;
using TaskBridge.Api.Notifications.Services;

namespace TaskBridge.Api.Notifications.Endpoints;

/// <summary>Audit-history filters applied to a project query.</summary>
public sealed record AuditHistoryQuery(DateTimeOffset? From, DateTimeOffset? To, string? EventType);

/// <summary>Internal request to record an audit event for an existing project.</summary>
public sealed record CreateAuditEventRequest(
    Guid ProjectId,
    string EventType,
    string? PreviousState,
    string? NewState);

/// <summary>An immutable audit event returned by the API.</summary>
public sealed record AuditEntryResponse(
    Guid Id,
    Guid ProjectId,
    string EntityType,
    string EventType,
    string ActorUserId,
    string? PreviousState,
    string? NewState,
    DateTimeOffset Timestamp,
    string? ActorIpAddress)
{
    public static AuditEntryResponse From(AuditEntry entry) => new(
        entry.Id,
        entry.ProjectId,
        entry.EntityType,
        entry.EventType,
        entry.ActorUserId,
        entry.PreviousState,
        entry.NewState,
        entry.Timestamp,
        entry.ActorIpAddress);
}

/// <summary>A notification belonging to the authenticated user.</summary>
public sealed record NotificationResponse(
    Guid Id,
    Guid ProjectId,
    string EventType,
    string Message,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt)
{
    public static NotificationResponse From(Notification notification) => new(
        notification.Id,
        notification.ProjectId,
        notification.EventType,
        notification.Message,
        notification.IsRead,
        notification.CreatedAt,
        notification.ReadAt);
}

public sealed class AuditHistoryQueryValidator : AbstractValidator<AuditHistoryQuery>
{
    public AuditHistoryQueryValidator()
    {
        RuleFor(query => query.EventType).MaximumLength(100);
        RuleFor(query => query)
            .Must(query => !query.From.HasValue || !query.To.HasValue || query.From.Value < query.To.Value)
            .WithMessage("The from timestamp must be earlier than the to timestamp.");
    }
}

public sealed class CreateAuditEventRequestValidator : AbstractValidator<CreateAuditEventRequest>
{
    public CreateAuditEventRequestValidator()
    {
        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.EventType)
            .Must(AuditService.IsSupportedEventType)
            .WithMessage("The audit event type is not supported.");
        RuleFor(request => request.PreviousState).MaximumLength(10000);
        RuleFor(request => request.NewState).MaximumLength(10000);
    }
}
