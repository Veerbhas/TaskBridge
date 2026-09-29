using FluentValidation;
using TaskBridge.Api.Projects.Models;

namespace TaskBridge.Api.Projects.Endpoints;

/// <summary>Request to create a project for a team.</summary>
public sealed record CreateProjectRequest(string Name, Guid TeamId);

/// <summary>Request to change a project's status.</summary>
public sealed record UpdateProjectStatusRequest(ProjectStatus? Status);

/// <summary>Project data returned by the API.</summary>
public sealed record ProjectResponse(
    Guid Id,
    string Name,
    ProjectStatus Status,
    Guid TeamId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static ProjectResponse From(Project project) => new(
        project.Id,
        project.Name,
        project.Status,
        project.TeamId,
        project.CreatedAt,
        project.UpdatedAt);
}

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.TeamId).NotEmpty();
    }
}

public sealed class UpdateProjectStatusRequestValidator : AbstractValidator<UpdateProjectStatusRequest>
{
    public UpdateProjectStatusRequestValidator() =>
        RuleFor(request => request.Status)
            .Must(status => status.HasValue && Enum.IsDefined(status.Value))
            .WithMessage("A valid project status is required.");
}