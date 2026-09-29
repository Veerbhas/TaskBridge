using Microsoft.EntityFrameworkCore;
using TaskBridge.Api.Data;

namespace TaskBridge.Api.Projects.Repositories;

public sealed class TeamMemberRepository(TaskBridgeDbContext dbContext) : ITeamMemberRepository
{
    public async Task<IReadOnlyList<string>> GetUserIdsByTeamAsync(
        Guid teamId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        await dbContext.TeamMembers
            .AsNoTracking()
            .Where(member => member.TeamId == teamId && member.OrganizationId == organizationId)
            .Select(member => member.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
}