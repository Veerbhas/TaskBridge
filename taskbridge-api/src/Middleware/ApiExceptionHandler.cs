using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TaskBridge.Api.Middleware;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", "Authentication is required."),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found", "The requested resource was not found."),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request", "The request contains invalid data."),
            _ => (0, string.Empty, string.Empty)
        };

        if (status == 0)
        {
            logger.LogError(exception, "Unhandled API exception for {Path}", httpContext.Request.Path);
            return false;
        }

        logger.LogWarning(exception, "Handled API exception with status {StatusCode}", status);
        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        }, cancellationToken);

        return true;
    }
}