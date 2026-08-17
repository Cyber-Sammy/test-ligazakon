using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using UserService.Api.Middleware;
using UserService.Application.Common;

namespace UserService.Tests.Api.Middleware;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task TryHandleAsync_UnexpectedException_ReturnsSanitizedProblemDetails()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var handler = new ExceptionHandlingMiddleware(
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("Sensitive database details."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body,
            JsonSerializerOptions.Web);
        Assert.NotNull(problemDetails);
        Assert.Equal(Constants.ProblemDetails.ServerErrorTitle, problemDetails.Title);
        Assert.Equal(Constants.ProblemDetails.UnexpectedErrorDetail, problemDetails.Detail);
        Assert.DoesNotContain("Sensitive", problemDetails.Detail);
    }
}
