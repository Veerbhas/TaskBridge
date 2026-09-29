using Microsoft.EntityFrameworkCore;
using TaskBridge.Api.Data;
using TaskBridge.Api.Projects.Models;

namespace TaskBridge.Api.Projects.Repositories;

public sealed class ProjectRepository(TaskBridgeDbContext dbContext) : IProjectRepository
{
    public Task<bool> TeamBelongsToOrganizationAsync(Guid teamId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.Teams.AnyAsync(team => team.Id == teamId && team.OrganizationId == organizationId, cancellationToken);

    public Task<Project?> FindByIdAsync(Guid projectId, Guid organizationId, CancellationToken cancellationToken = default) =>
        dbContext.Projects.FirstOrDefaultAsync(
            project => project.Id == projectId && project.OrganizationId == organizationId,
            cancellationToken);

    public async Task<IReadOnlyList<Project>> GetByTeamAsync(Guid teamId, Guid organizationId, CancellationToken cancellationToken = default) =>
        await dbContext.Projects
            .AsNoTracking()
            .Where(project => project.TeamId == teamId && project.OrganizationId == organizationId)
            .OrderBy(project => project.Name)
            .ToListAsync(cancellationToken);

    public void Add(Project project) => dbContext.Projects.Add(project);

    public void Remove(Project project) => dbContext.Projects.Remove(project);
}