namespace TaskBridge.Api.Projects.Repositories;

public interface ITeamMemberRepository
{
    Task<IReadOnlyList<string>> GetUserIdsByTeamAsync(
        Guid teamId,
        Guid organizationId,
        CancellationToken cancellationToken = default);
}