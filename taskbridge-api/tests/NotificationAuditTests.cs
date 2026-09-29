using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TaskBridge.Api.Data;
using TaskBridge.Api.Notifications.Endpoints;
using TaskBridge.Api.Notifications.Models;
using TaskBridge.Api.Notifications.Repositories;
using TaskBridge.Api.Notifications.Services;
using TaskBridge.Api.Projects.Models;
using TaskBridge.Api.Projects.Repositories;
using TaskBridge.Api.Projects.Services;
using Xunit;

namespace TaskBridge.Api.Tests;

public sealed class NotificationAuditTests
{
    [Fact]
    public async Task InternalAuditEvent_UsesCurrentActorAndOrganization()
    {
        await using var fixture = new FeatureFixture();
        var project = await fixture.ProjectService.CreateAsync("TaskBridge", fixture.TeamId);

        var entry = await fixture.AuditService.RecordInternalEventAsync(
            project.Id,
            "MILESTONE_REOPENED",
            "{\"status\":\"Closed\"}",
            "{\"status\":\"Open\"}");

        entry.Should().NotBeNull();
        entry!.ActorUserId.Should().Be(fixture.UserId);
        entry.OrganizationId.Should().Be(fixture.OrganizationId);
        entry.EventType.Should().Be("MILESTONE_REOPENED");
    }


    [Fact]
    public async Task InternalAuditEvent_DoesNotFindAnotherOrganizationsProject()
    {
        await using var fixture = new FeatureFixture();
        var foreignProject = new Project("Foreign", fixture.TeamId, Guid.NewGuid(), DateTimeOffset.UtcNow);
        fixture.DbContext.Projects.Add(foreignProject);
        await fixture.DbContext.SaveChangesAsync();

        var entry = await fixture.AuditService.RecordInternalEventAsync(
            foreignProject.Id,
            "PROJECT_UPDATED",
            "{}",
            "{}");

        entry.Should().BeNull();
        (await fixture.DbContext.AuditEntries.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task InternalAuditValidator_RejectsUnsupportedEventType()
    {
        var validator = new CreateAuditEventRequestValidator();

        var result = await validator.ValidateAsync(new CreateAuditEventRequest(
            Guid.NewGuid(),
            "UNSUPPORTED_EVENT",
            null,
            null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ProjectCreated_QueuesNotificationForEveryTeamMember()
    {
        await using var fixture = new FeatureFixture();

        var project = await fixture.ProjectService.CreateAsync("TaskBridge", fixture.TeamId);

        var notifications = await fixture.DbContext.Notifications
            .Where(item => item.ProjectId == project.Id)
            .ToListAsync();
        notifications.Should().HaveCount(2);
        notifications.Select(item => item.RecipientUserId)
            .Should().BeEquivalentTo(fixture.UserId, fixture.OtherUserId);
    }

    [Fact]
    public async Task MilestoneUpdated_CreatesAuditEntryWithPreviousAndNewState()
    {
        await using var fixture = new FeatureFixture();
        var project = await fixture.ProjectService.CreateAsync("TaskBridge", fixture.TeamId);
        const string previousState = "{\"status\":\"InProgress\"}";
        const string newState = "{\"status\":\"Completed\"}";

        var entry = await fixture.AuditService.RecordInternalEventAsync(
            project.Id,
            "MILESTONE_UPDATED",
            previousState,
            newState);

        entry.Should().NotBeNull();
        entry!.ProjectId.Should().Be(project.Id);
        entry.EventType.Should().Be("MILESTONE_UPDATED");
        entry.PreviousState.Should().Be(previousState);
        entry.NewState.Should().Be(newState);
        entry.ActorUserId.Should().Be(fixture.UserId);
        entry.OrganizationId.Should().Be(fixture.OrganizationId);
    }

    [Fact]
    public async Task ProjectStatusChanged_QueuesNotificationForEveryTeamMember()
    {
        await using var fixture = new FeatureFixture();
        var project = await fixture.ProjectService.CreateAsync("TaskBridge", fixture.TeamId);

        var updated = await fixture.ProjectService.UpdateStatusAsync(project.Id, ProjectStatus.OnHold);

        updated!.Status.Should().Be(ProjectStatus.OnHold);
        var notifications = await fixture.DbContext.Notifications
            .Where(item => item.ProjectId == project.Id && item.EventType == "PROJECT_UPDATED")
            .ToListAsync();
        notifications.Should().HaveCount(2);
        notifications.Select(item => item.RecipientUserId)
            .Should().BeEquivalentTo(fixture.UserId, fixture.OtherUserId);
        notifications.Should().OnlyContain(item => item.Message.Contains("OnHold", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UnreadNotifications_AreLimitedToCurrentRecipientAndOrganization()
    {
        await using var fixture = new FeatureFixture();
        var project = await fixture.ProjectService.CreateAsync("TaskBridge", fixture.TeamId);
        fixture.DbContext.Notifications.Add(new Notification(
            fixture.UserId,
            project.Id,
            "PROJECT_CREATED",
            "Other tenant notification",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow));
        fixture.DbContext.Notifications.Add(new Notification(
            fixture.OtherUserId,
            project.Id,
            "PROJECT_CREATED",
            "Other recipient notification",
            fixture.OrganizationId,
            DateTimeOffset.UtcNow));
        await fixture.DbContext.SaveChangesAsync();

        var unread = await fixture.NotificationService.GetUnreadForCurrentUserAsync();

        unread.Should().ContainSingle().Which.RecipientUserId.Should().Be(fixture.UserId);
    }

    [Fact]
    public async Task MarkRead_RejectsNotificationOwnedByAnotherUser()
    {
        await using var fixture = new FeatureFixture();
        var project = await fixture.ProjectService.CreateAsync("TaskBridge", fixture.TeamId);
        var notification = await fixture.DbContext.Notifications
            .SingleAsync(item => item.RecipientUserId == fixture.UserId);
        var otherUserService = fixture.CreateNotificationService(fixture.OtherUserId);

        var marked = await otherUserService.MarkReadForCurrentUserAsync(notification.Id);

        marked.Should().BeFalse();
        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task MarkRead_UpdatesNotificationOwnedByCurrentUser()
    {
        await using var fixture = new FeatureFixture();
        var project = await fixture.ProjectService.CreateAsync("TaskBridge", fixture.TeamId);
        var notification = await fixture.DbContext.Notifications
            .SingleAsync(item => item.RecipientUserId == fixture.UserId);

        var marked = await fixture.NotificationService.MarkReadForCurrentUserAsync(notification.Id);

        marked.Should().BeTrue();
        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();
        (await fixture.NotificationService.GetUnreadForCurrentUserAsync())
            .Should().NotContain(item => item.Id == notification.Id);
    }

    [Fact]
    public async Task AuditHistory_FiltersByDateRange()
    {
        await using var fixture = new FeatureFixture();
        var project = await fixture.ProjectService.CreateAsync("TaskBridge", fixture.TeamId);
        var firstTimestamp = DateTimeOffset.UtcNow.AddDays(-2);
        var lastTimestamp = DateTimeOffset.UtcNow.AddDays(-1);
        fixture.AuditService.RecordProjectEvent(project, "MILESTONE_CREATED", null, "{}", firstTimestamp);
        fixture.AuditService.RecordProjectEvent(project, "MILESTONE_CLOSED", "{}", "{}", lastTimestamp);
        await fixture.DbContext.SaveChangesAsync();

        var entries = await fixture.AuditService.GetProjectHistoryAsync(
            project.Id,
            firstTimestamp,
            lastTimestamp,
            null);

        entries.Should().ContainSingle().Which.EventType.Should().Be("MILESTONE_CREATED");
    }

    [Fact]
    public async Task AuditHistory_FiltersByEventType()
    {
        await using var fixture = new FeatureFixture();
        var project = await fixture.ProjectService.CreateAsync("TaskBridge", fixture.TeamId);
        fixture.AuditService.RecordProjectEvent(project, "MILESTONE_CREATED", null, "{}", DateTimeOffset.UtcNow);
        fixture.AuditService.RecordProjectEvent(project, "MILESTONE_CLOSED", "{}", "{}", DateTimeOffset.UtcNow.AddSeconds(1));
        await fixture.DbContext.SaveChangesAsync();

        var entries = await fixture.AuditService.GetProjectHistoryAsync(project.Id, null, null, "MILESTONE_CLOSED");

        entries.Should().ContainSingle().Which.EventType.Should().Be("MILESTONE_CLOSED");
    }

    [Fact]
    public async Task AuditHistory_DoesNotReturnAnotherOrganizationsEvents()
    {
        await using var fixture = new FeatureFixture();
        var project = await fixture.ProjectService.CreateAsync("TaskBridge", fixture.TeamId);
        fixture.DbContext.AuditEntries.Add(new AuditEntry(
            project.Id,
            "PROJECT_DELETED",
            fixture.UserId,
            Guid.NewGuid(),
            "{}",
            null,
            DateTimeOffset.UtcNow,
            null));
        await fixture.DbContext.SaveChangesAsync();

        var entries = await fixture.AuditService.GetProjectHistoryAsync(project.Id, null, null, null);

        entries.Should().ContainSingle().Which.EventType.Should().Be("PROJECT_CREATED");
    }

    [Fact]
    public async Task AuditHistoryValidator_RejectsReversedDateRange()
    {
        var validator = new AuditHistoryQueryValidator();
        var now = DateTimeOffset.UtcNow;

        var result = await validator.ValidateAsync(new AuditHistoryQuery(now, now.AddSeconds(-1), null));

        result.IsValid.Should().BeFalse();
    }

    private sealed class FeatureFixture : IAsyncDisposable
    {
        private readonly TestActorContext _actor;

        public FeatureFixture()
        {
            OrganizationId = Guid.NewGuid();
            TeamId = Guid.NewGuid();
            UserId = "user-current";
            OtherUserId = "user-other";
            _actor = new TestActorContext(OrganizationId, UserId);

            var options = new DbContextOptionsBuilder<TaskBridgeDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            DbContext = new TaskBridgeDbContext(options);
            DbContext.Teams.Add(new Team(TeamId, OrganizationId));
            DbContext.TeamMembers.AddRange(
                new TeamMember(TeamId, OrganizationId, UserId),
                new TeamMember(TeamId, OrganizationId, OtherUserId));
            DbContext.SaveChanges();

            ProjectRepository = new ProjectRepository(DbContext);
            AuditRepository = new AuditRepository(DbContext);
            NotificationRepository = new NotificationRepository(DbContext);
            TeamMemberRepository = new TeamMemberRepository(DbContext);
            AuditService = new AuditService(
                AuditRepository,
                ProjectRepository,
                DbContext,
                _actor,
                TimeProvider.System);
            NotificationService = CreateNotificationService(UserId);
            ProjectService = new ProjectService(
                ProjectRepository,
                AuditService,
                NotificationService,
                DbContext,
                _actor,
                TimeProvider.System,
                NullLogger<ProjectService>.Instance);
        }

        public Guid OrganizationId { get; }

        public Guid TeamId { get; }

        public string UserId { get; }

        public string OtherUserId { get; }

        public TaskBridgeDbContext DbContext { get; }

        public ProjectRepository ProjectRepository { get; }

        public AuditRepository AuditRepository { get; }

        public NotificationRepository NotificationRepository { get; }

        public TeamMemberRepository TeamMemberRepository { get; }

        public AuditService AuditService { get; }

        public NotificationService NotificationService { get; }

        public IProjectService ProjectService { get; }

        public NotificationService CreateNotificationService(string userId) => new(
            NotificationRepository,
            TeamMemberRepository,
            DbContext,
            new TestActorContext(OrganizationId, userId),
            TimeProvider.System,
            NullLogger<NotificationService>.Instance);

        public ValueTask DisposeAsync() => DbContext.DisposeAsync();
    }

    private sealed class TestActorContext(Guid organizationId, string userId) : ICurrentActorContext
    {
        public Guid OrganizationId { get; } = organizationId;

        public string UserId { get; } = userId;

        public string? IpAddress => "127.0.0.1";
    }
}
