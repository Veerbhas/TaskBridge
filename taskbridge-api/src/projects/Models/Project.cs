namespace TaskBridge.Api.Projects.Models;

public enum ProjectStatus
{
    Active,
    OnHold,
    Completed
}

public sealed class Project
{
    private Project()
    {
        Name = string.Empty;
    }

    public Project(string name, Guid teamId, Guid organizationId, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedName = name.Trim();
        if (normalizedName.Length > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(name), "Project names cannot exceed 200 characters.");
        }

        if (teamId == Guid.Empty)
        {
            throw new ArgumentException("Team ID cannot be empty.", nameof(teamId));
        }

        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization ID cannot be empty.", nameof(organizationId));
        }

        Id = Guid.NewGuid();
        Name = normalizedName;
        TeamId = teamId;
        OrganizationId = organizationId;
        Status = ProjectStatus.Active;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public ProjectStatus Status { get; private set; }

    public Guid TeamId { get; private set; }

    public Guid OrganizationId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void ChangeStatus(ProjectStatus status, DateTimeOffset updatedAt)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "The project status is not valid.");
        }

        Status = status;
        UpdatedAt = updatedAt;
    }
}