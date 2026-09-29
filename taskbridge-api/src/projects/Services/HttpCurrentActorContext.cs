using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace TaskBridge.Api.Projects.Services;

public sealed class HttpCurrentActorContext(IHttpContextAccessor httpContextAccessor) : ICurrentActorContext
{
    private ClaimsPrincipal User =>
        httpContextAccessor.HttpContext?.User
        ?? throw new UnauthorizedAccessException("An authenticated request is required.");

    public Guid OrganizationId
    {
        get
        {
            var claim = User.FindFirst("organizationId")?.Value;
            if (!Guid.TryParse(claim, out var organizationId) || organizationId == Guid.Empty)
            {
                throw new UnauthorizedAccessException("The authenticated user has no valid organization claim.");
            }

            return organizationId;
        }
    }

    public string UserId
    {
        get
        {
            var userId = User.FindFirst("sub")?.Value;
            return !string.IsNullOrWhiteSpace(userId)
                ? userId
                : throw new UnauthorizedAccessException("The authenticated user has no subject claim.");
        }
    }

    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}