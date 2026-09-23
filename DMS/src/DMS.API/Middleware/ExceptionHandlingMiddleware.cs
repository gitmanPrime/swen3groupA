using DMS.BLL.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace DMS.API.Middleware;

/// <summary>
/// Converts exceptions thrown by the application into consistent
/// RFC 7807 Problem Details responses and logs unexpected failures.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Not Found", ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, "Conflict", ex.Message);
        }
        catch (ValidationException ex)
        {
            await WriteValidationProblemAsync(context, ex.Errors);
        }
        catch (BadHttpRequestException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Bad Request", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An unhandled exception occurred while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred. Please try again later.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };

        await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
    }

    private static async Task WriteValidationProblemAsync(HttpContext context, IReadOnlyDictionary<string, string[]> errors)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";

        var problem = new ValidationProblemDetails(new Dictionary<string, string[]>(errors))
        {
            Title = "Validation Failed",
            Status = StatusCodes.Status400BadRequest
        };

        await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
    }
}