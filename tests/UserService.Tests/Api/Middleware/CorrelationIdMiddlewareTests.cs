using Microsoft.AspNetCore.Http;
using UserService.Api.Middleware;
using UserService.Application.Common;

namespace UserService.Tests.Api.Middleware;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ValidRequestHeader_PreservesCorrelationId()
    {
        const string correlationId = "client-correlation-id";
        var context = CreateContext();
        context.Request.Headers[Constants.Correlation.HeaderName] = correlationId;
        string? downstreamTraceIdentifier = null;
        var middleware = new CorrelationIdMiddleware(async currentContext =>
        {
            downstreamTraceIdentifier = currentContext.TraceIdentifier;
            await currentContext.Response.WriteAsync("ok");
        });

        await middleware.InvokeAsync(context);

        Assert.Equal(correlationId, downstreamTraceIdentifier);
        Assert.Equal(correlationId, context.TraceIdentifier);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InvokeAsync_MissingOrBlankHeader_GeneratesCorrelationId(string? value)
    {
        var context = CreateContext();
        if (value is not null)
        {
            context.Request.Headers[Constants.Correlation.HeaderName] = value;
        }

        await InvokeMiddleware(context);

        Assert.True(Guid.TryParseExact(
            context.TraceIdentifier,
            Constants.Correlation.IdFormat,
            out _));
    }

    [Fact]
    public async Task InvokeAsync_MultipleHeaderValues_GeneratesCorrelationId()
    {
        var context = CreateContext();
        context.Request.Headers[Constants.Correlation.HeaderName] =
            new[] { "first", "second" };

        await InvokeMiddleware(context);

        Assert.True(Guid.TryParseExact(
            context.TraceIdentifier,
            Constants.Correlation.IdFormat,
            out _));
    }

    [Fact]
    public async Task InvokeAsync_OverlongHeader_GeneratesCorrelationId()
    {
        var context = CreateContext();
        context.Request.Headers[Constants.Correlation.HeaderName] = new string('a', 129);

        await InvokeMiddleware(context);

        Assert.NotEqual(new string('a', 129), context.TraceIdentifier);
        Assert.True(Guid.TryParseExact(
            context.TraceIdentifier,
            Constants.Correlation.IdFormat,
            out _));
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static Task InvokeMiddleware(HttpContext context) =>
        new CorrelationIdMiddleware(currentContext =>
            currentContext.Response.WriteAsync("ok"))
        .InvokeAsync(context);
}
