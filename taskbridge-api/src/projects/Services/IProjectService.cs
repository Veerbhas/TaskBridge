using TaskBridge.Api.Projects.Models;

namespace TaskBridge.Api.Projects.Services;

public interface IProjectService
{
    Task<Project> CreateAsync(string name, Guid teamId, CancellationToken cancellationToken = default);

    Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<Project?> UpdateStatusAsync(Guid projectId, ProjectStatus status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Project>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid projectId, CancellationToken cancellationToken = default);
}