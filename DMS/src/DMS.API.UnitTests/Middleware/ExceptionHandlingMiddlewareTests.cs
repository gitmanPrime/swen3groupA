using System.Text.Json;
using DMS.API.Middleware;
using DMS.BLL.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace DMS.API.UnitTests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    public static IEnumerable<object[]> MappedExceptions =>
    [
        [new NotFoundException("Document", Guid.NewGuid()), 404, "Not Found"],
        [new ConflictException("Already exists."), 409, "Conflict"],
        [new ValidationException("fileName", "Required."), 400, "Validation Failed"],
        [new MessagingException("RabbitMQ is unavailable."), 503, "Messaging Service Error"],
        [new BadHttpRequestException("Malformed request."), 400, "Bad Request"]
    ];

    [Theory]
    [MemberData(nameof(MappedExceptions))]
    public async Task InvokeAsync_MapsKnownExceptions(
        Exception exception, int expectedStatus, string expectedTitle)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => Task.FromException(exception),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(expectedStatus, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(expectedTitle, body.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task InvokeAsync_UnexpectedException_ReturnsSafe500Response()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => Task.FromException(new IOException("RabbitMQ credentials leaked")),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("Internal Server Error", body.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            "An unexpected error occurred. Please try again later.",
            body.RootElement.GetProperty("detail").GetString());
    }
}