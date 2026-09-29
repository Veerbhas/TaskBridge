using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TaskBridge.Api.Data;
using TaskBridge.Api.Notifications.Models;
using TaskBridge.Api.Notifications.Repositories;
using TaskBridge.Api.Notifications.Services;
using TaskBridge.Api.Projects.Models;
using TaskBridge.Api.Projects.Endpoints;
using TaskBridge.Api.Projects.Repositories;
using TaskBridge.Api.Projects.Services;
using Xunit;

namespace TaskBridge.Api.Tests;

public sealed class ProjectServiceTests
{
    [Fact]
    public async Task UpdateStatusValidator_RejectsMissingStatus()
    {
        var validator = new UpdateProjectStatusRequestValidator();

        var result = await validator.ValidateAsync(new UpdateProjectStatusRequest(null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_UsesCurrentOrganizationAndWritesAuditEntry()
    {
        await using var fixture = new ProjectFixture();

        var project = await fixture.Service.CreateAsync("  TaskBridge  ", fixture.TeamId);

        project.Name.Should().Be("TaskBridge");
        project.OrganizationId.Should().Be(fixture.OrganizationId);
        var audit = await fixture.DbContext.AuditEntries.SingleAsync();
        audit.EventType.Should().Be("PROJECT_CREATED");
        audit.ActorUserId.Should().Be(fixture.UserId);
        audit.NewState.Should().Contain("TaskBridge");
    }

    [Fact]
    public async Task CreateAsync_RejectsTeamFromAnotherOrganization()
    {
        await using var fixture = new ProjectFixture();
        var foreignTeamId = Guid.NewGuid();
        fixture.DbContext.Teams.Add(new Team(foreignTeamId, Guid.NewGuid()));
        await fixture.DbContext.SaveChangesAsync();

        var action = () => fixture.Service.CreateAsync("TaskBridge", foreignTeamId);

        await action.Should().ThrowAsync<KeyNotFoundException>();
        (await fixture.DbContext.Projects.CountAsync()).Should().Be(0);
        (await fixture.DbContext.AuditEntries.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task UpdateStatusAsync_RecordsPreviousAndNewState()
    {
        await using var fixture = new ProjectFixture();
        var project = await fixture.Service.CreateAsync("TaskBridge", fixture.TeamId);

        var updated = await fixture.Service.UpdateStatusAsync(project.Id, ProjectStatus.OnHold);

        updated!.Status.Should().Be(ProjectStatus.OnHold);
        var audit = await fixture.DbContext.AuditEntries.SingleAsync(entry => entry.EventType == "PROJECT_UPDATED");
        audit.PreviousState.Should().Contain("Active");
        audit.NewState.Should().Contain("OnHold");
    }

    [Fact]
    public async Task UpdateStatusAsync_DoesNotExposeAnotherOrganizationProject()
    {
        await using var fixture = new ProjectFixture();
        var project = new Project("Foreign", fixture.TeamId, Guid.NewGuid(), DateTimeOffset.UtcNow);
        fixture.DbContext.Projects.Add(project);
        await fixture.DbContext.SaveChangesAsync();

        var updated = await fixture.Service.UpdateStatusAsync(project.Id, ProjectStatus.Completed);

        updated.Should().BeNull();
        project.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public async Task GetByTeamAsync_ReturnsOnlyCurrentOrganizationProjects()
    {
        await using var fixture = new ProjectFixture();
        var local = await fixture.Service.CreateAsync("Local", fixture.TeamId);
        var otherOrganizationId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();
        fixture.DbContext.Teams.Add(new Team(otherTeamId, otherOrganizationId));
        fixture.DbContext.Projects.Add(new Project("Foreign", otherTeamId, otherOrganizationId, DateTimeOffset.UtcNow));
        await fixture.DbContext.SaveChangesAsync();

        var projects = await fixture.Service.GetByTeamAsync(fixture.TeamId);

        projects.Should().ContainSingle().Which.Id.Should().Be(local.Id);
    }

    [Fact]
    public async Task DeleteAsync_DeletesProjectAndPersistsAuditEntry()
    {
        await using var fixture = new ProjectFixture();
        var project = await fixture.Service.CreateAsync("TaskBridge", fixture.TeamId);

        var deleted = await fixture.Service.DeleteAsync(project.Id);

        deleted.Should().BeTrue();
        (await fixture.DbContext.Projects.AnyAsync(item => item.Id == project.Id)).Should().BeFalse();
        (await fixture.DbContext.AuditEntries.CountAsync(entry => entry.EventType == "PROJECT_DELETED")).Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_RejectsAuditUpdates()
    {
        await using var fixture = new ProjectFixture();
        await fixture.Service.CreateAsync("TaskBridge", fixture.TeamId);
        var audit = await fixture.DbContext.AuditEntries.SingleAsync();
        fixture.DbContext.Entry(audit).Property(entry => entry.EventType).CurrentValue = "TAMPERED";

        var action = () => fixture.DbContext.SaveChangesAsync();

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Audit entries cannot be updated or deleted.");
    }

    [Fact]
    public async Task SaveChangesAsync_RejectsAuditDeletes()
    {
        await using var fixture = new ProjectFixture();
        await fixture.Service.CreateAsync("TaskBridge", fixture.TeamId);
        var audit = await fixture.DbContext.AuditEntries.SingleAsync();
        fixture.DbContext.AuditEntries.Remove(audit);

        var action = () => fixture.DbContext.SaveChangesAsync();

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Audit entries cannot be updated or deleted.");
    }

    [Fact]
    public async Task SaveChangesWithAcceptFlag_RejectsAuditDeletes()
    {
        await using var fixture = new ProjectFixture();
        await fixture.Service.CreateAsync("TaskBridge", fixture.TeamId);
        var audit = await fixture.DbContext.AuditEntries.SingleAsync();
        fixture.DbContext.AuditEntries.Remove(audit);

        var action = () => fixture.DbContext.SaveChangesAsync(acceptAllChangesOnSuccess: false);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Audit entries cannot be updated or deleted.");
    }

    private sealed class ProjectFixture : IAsyncDisposable
    {
        public ProjectFixture()
        {
            OrganizationId = Guid.NewGuid();
            TeamId = Guid.NewGuid();
            UserId = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<TaskBridgeDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            DbContext = new TaskBridgeDbContext(options);
            DbContext.Teams.Add(new Team(TeamId, OrganizationId));
            DbContext.SaveChanges();

            Service = new ProjectService(
                new ProjectRepository(DbContext),
                new AuditService(
                    new AuditRepository(DbContext),
                    new ProjectRepository(DbContext),
                    DbContext,
                    new TestActorContext(OrganizationId, UserId),
                    TimeProvider.System),
                new NotificationService(
                    new NotificationRepository(DbContext),
                    new TeamMemberRepository(DbContext),
                    DbContext,
                    new TestActorContext(OrganizationId, UserId),
                    TimeProvider.System,
                    NullLogger<NotificationService>.Instance),
                DbContext,
                new TestActorContext(OrganizationId, UserId),
                TimeProvider.System,
                NullLogger<ProjectService>.Instance);
        }

        public Guid OrganizationId { get; }

        public Guid TeamId { get; }

        public string UserId { get; }

        public TaskBridgeDbContext DbContext { get; }

        public IProjectService Service { get; }

        public ValueTask DisposeAsync() => DbContext.DisposeAsync();
    }

    private sealed class TestActorContext(Guid organizationId, string userId) : ICurrentActorContext
    {
        public Guid OrganizationId { get; } = organizationId;

        public string UserId { get; } = userId;

        public string? IpAddress => "127.0.0.1";
    }
}