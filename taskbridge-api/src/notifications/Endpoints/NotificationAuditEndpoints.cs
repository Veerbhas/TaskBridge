using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using TaskBridge.Api.Notifications.Services;
using TaskBridge.Api.Projects.Services;

namespace TaskBridge.Api.Notifications.Endpoints;

public static class NotificationAuditEndpoints
{
    public static IEndpointRouteBuilder MapNotificationAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/audit", CreateInternalAuditEventAsync)
            .RequireAuthorization("AuditWriter")
            .WithName("CreateInternalAuditEvent")
            .WithTags("Audit")
            .WithSummary("Record an internal audit event")
            .WithDescription("For trusted internal callers only. Actor and organization values come from the validated JWT.")
            .Produces<AuditEntryResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        endpoints.MapGet("/audit/{projectId:guid}", GetAuditHistoryAsync)
            .RequireAuthorization("AuditReader")
            .WithName("GetProjectAuditHistory")
            .WithTags("Audit")
            .WithSummary("Get project audit history")
            .WithDescription("Returns immutable audit events for a project in the authenticated organization.")
            .Produces<IReadOnlyList<AuditEntryResponse>>()
            .ProducesValidationProblem();

        endpoints.MapGet("/notifications/{userId}", GetUnreadNotificationsAsync)
            .RequireAuthorization("NotificationReader")
            .WithName("GetUnreadNotifications")
            .WithTags("Notifications")
            .WithSummary("Get the authenticated user's unread notifications")
            .WithDescription("The requested user ID must match the subject of the validated JWT.")
            .Produces<IReadOnlyList<NotificationResponse>>()
            .Produces(StatusCodes.Status403Forbidden);

        endpoints.MapPatch("/notifications/{notificationId:guid}/read", MarkNotificationReadAsync)
            .RequireAuthorization("NotificationReader")
            .WithName("MarkNotificationRead")
            .WithTags("Notifications")
            .WithSummary("Mark one of the authenticated user's notifications as read")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<Results<Created<AuditEntryResponse>, ValidationProblem, NotFound>> CreateInternalAuditEventAsync(
        CreateAuditEventRequest request,
        IValidator<CreateAuditEventRequest> validator,
        IAuditService auditService,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(ToErrors(validation));
        }

        var entry = await auditService.RecordInternalEventAsync(
            request.ProjectId,
            request.EventType,
            request.PreviousState,
            request.NewState,
            cancellationToken);
        if (entry is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Created($"/audit/{entry.ProjectId}", AuditEntryResponse.From(entry));
    }

    private static async Task<Results<Ok<IReadOnlyList<AuditEntryResponse>>, ValidationProblem>> GetAuditHistoryAsync(
        Guid projectId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? eventType,
        IValidator<AuditHistoryQuery> validator,
        IAuditService auditService,
        CancellationToken cancellationToken)
    {
        var query = new AuditHistoryQuery(from, to, eventType);
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(ToErrors(validation));
        }

        var entries = await auditService.GetProjectHistoryAsync(
            projectId,
            from,
            to,
            eventType,
            cancellationToken);
        var response = entries.Select(AuditEntryResponse.From).ToArray();
        return TypedResults.Ok<IReadOnlyList<AuditEntryResponse>>(response);
    }

    private static async Task<Results<Ok<IReadOnlyList<NotificationResponse>>, ForbidHttpResult>> GetUnreadNotificationsAsync(
        string userId,
        ICurrentActorContext currentActor,
        INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(userId, currentActor.UserId, StringComparison.Ordinal))
        {
            return TypedResults.Forbid();
        }

        var notifications = await notificationService.GetUnreadForCurrentUserAsync(cancellationToken);
        var response = notifications.Select(NotificationResponse.From).ToArray();
        return TypedResults.Ok<IReadOnlyList<NotificationResponse>>(response);
    }

    private static async Task<Results<NoContent, NotFound>> MarkNotificationReadAsync(
        Guid notificationId,
        INotificationService notificationService,
        CancellationToken cancellationToken) =>
        await notificationService.MarkReadForCurrentUserAsync(notificationId, cancellationToken)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();

    private static IDictionary<string, string[]> ToErrors(ValidationResult validation) =>
        validation.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
}
