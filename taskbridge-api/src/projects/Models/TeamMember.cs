namespace TaskBridge.Api.Projects.Models;

public sealed class TeamMember
{
    private TeamMember()
    {
        UserId = string.Empty;
    }

    public TeamMember(Guid teamId, Guid organizationId, string userId)
    {
        if (teamId == Guid.Empty || organizationId == Guid.Empty)
        {
            throw new ArgumentException("Team and organization IDs must be non-empty.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (userId.Length > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "User IDs cannot exceed 200 characters.");
        }

        TeamId = teamId;
        OrganizationId = organizationId;
        UserId = userId;
    }

    public Guid TeamId { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string UserId { get; private set; }
}
