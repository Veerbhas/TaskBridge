using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using TaskBridge.Api.Projects.Models;
using TaskBridge.Api.Projects.Services;

namespace TaskBridge.Api.Projects.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var projects = endpoints.MapGroup("/projects").WithTags("Projects");

        projects.MapPost("/", CreateAsync)
            .RequireAuthorization("ProjectWriter")
            .WithName("CreateProject")
            .WithSummary("Create a project")
            .WithDescription("Creates a project for a team in the authenticated user's organization.")
            .Produces<ProjectResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        projects.MapGet("/{projectId:guid}", GetByIdAsync)
            .RequireAuthorization("ProjectReader")
            .WithName("GetProject")
            .WithSummary("Get a project")
            .Produces<ProjectResponse>()
            .Produces(StatusCodes.Status404NotFound);

        projects.MapPatch("/{projectId:guid}/status", UpdateStatusAsync)
            .RequireAuthorization("ProjectWriter")
            .WithName("UpdateProjectStatus")
            .WithSummary("Update a project's status")
            .Produces<ProjectResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        projects.MapGet("/team/{teamId:guid}", GetByTeamAsync)
            .RequireAuthorization("ProjectReader")
            .WithName("GetProjectsByTeam")
            .WithSummary("List projects for a team")
            .Produces<IReadOnlyList<ProjectResponse>>();

        projects.MapDelete("/{projectId:guid}", DeleteAsync)
            .RequireAuthorization("ProjectWriter")
            .WithName("DeleteProject")
            .WithSummary("Delete a project")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<Results<Created<ProjectResponse>, ValidationProblem>> CreateAsync(
        CreateProjectRequest request,
        IValidator<CreateProjectRequest> validator,
        IProjectService projectService,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(ToErrors(validation));
        }

        var project = await projectService.CreateAsync(request.Name, request.TeamId, cancellationToken);
        var response = ProjectResponse.From(project);
        return TypedResults.Created($"/projects/{project.Id}", response);
    }

    private static async Task<Results<Ok<ProjectResponse>, NotFound>> GetByIdAsync(
        Guid projectId,
        IProjectService projectService,
        CancellationToken cancellationToken)
    {
        var project = await projectService.GetByIdAsync(projectId, cancellationToken);
        return project is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ProjectResponse.From(project));
    }

    private static async Task<Results<Ok<ProjectResponse>, NotFound, ValidationProblem>> UpdateStatusAsync(
        Guid projectId,
        UpdateProjectStatusRequest request,
        IValidator<UpdateProjectStatusRequest> validator,
        IProjectService projectService,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(ToErrors(validation));
        }

        var project = await projectService.UpdateStatusAsync(projectId, request.Status!.Value, cancellationToken);
        return project is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ProjectResponse.From(project));
    }

    private static async Task<Ok<IReadOnlyList<ProjectResponse>>> GetByTeamAsync(
        Guid teamId,
        IProjectService projectService,
        CancellationToken cancellationToken)
    {
        var projects = (await projectService.GetByTeamAsync(teamId, cancellationToken))
            .Select(ProjectResponse.From)
            .ToArray();
        return TypedResults.Ok<IReadOnlyList<ProjectResponse>>(projects);
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        Guid projectId,
        IProjectService projectService,
        CancellationToken cancellationToken) =>
        await projectService.DeleteAsync(projectId, cancellationToken)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();

    private static IDictionary<string, string[]> ToErrors(ValidationResult validation) =>
        validation.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
}