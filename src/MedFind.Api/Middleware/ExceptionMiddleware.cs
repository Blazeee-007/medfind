using MedFind.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace MedFind.Api.Middleware;

/// <summary>
/// Global exception handler → RFC 7807 ProblemDetails.
/// Never leaks stack traces or EF entities.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleAsync(ctx, ex);
        }
    }

    private static async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        var (status, title) = ex switch
        {
            ConflictException     => (StatusCodes.Status409Conflict,    "Conflict"),
            NotFoundException     => (StatusCodes.Status404NotFound,    "Not Found"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized,"Unauthorized"),
            ForbiddenException    => (StatusCodes.Status403Forbidden,   "Forbidden"),
            _                     => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        var detail = ex is ConflictException or NotFoundException or UnauthorizedException or ForbiddenException
            ? ex.Message
            : "An unexpected error occurred. Please try again.";

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
        };

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsJsonAsync(problem);
    }
}
