using TaskBridge.Api.Projects.Models;

namespace TaskBridge.Api.Projects.Repositories;

public interface IProjectRepository
{
    Task<bool> TeamBelongsToOrganizationAsync(Guid teamId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<Project?> FindByIdAsync(Guid projectId, Guid organizationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Project>> GetByTeamAsync(Guid teamId, Guid organizationId, CancellationToken cancellationToken = default);

    void Add(Project project);

    void Remove(Project project);
}