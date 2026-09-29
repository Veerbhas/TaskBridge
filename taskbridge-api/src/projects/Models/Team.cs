namespace TaskBridge.Api.Projects.Models;

public sealed class Team
{
    private Team()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Team(Guid id, Guid organizationId)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty)
        {
            throw new ArgumentException("Team and organization IDs must be non-empty.");
        }

        Id = id;
        OrganizationId = organizationId;
    }
}