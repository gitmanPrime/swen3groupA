using DMS.BLL.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DMS.API
{
    public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            logger.LogError(exception, "An unhandled exception occured: {Message}", exception.Message);

            var (statusCode, title, detail) = exception switch
            {
                NotFoundException notFound => (StatusCodes.Status404NotFound, "Not found", notFound.Message),
                ConflictException conflict => (StatusCodes.Status409Conflict, "Conflict", conflict.Message),
                ValidationException validation => (StatusCodes.Status400BadRequest, "Validation Error", validation.Message),
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An internal error occured.")
            };

            httpContext.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            };

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }
    }
}
