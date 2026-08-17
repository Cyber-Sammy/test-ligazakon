using Serilog.Context;
using UserService.Application.Common;

namespace UserService.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const int MaxCorrelationIdLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[Constants.Correlation.HeaderName] = correlationId;

            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(Constants.Correlation.LogPropertyName, correlationId))
        {
            await next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(Constants.Correlation.HeaderName, out var headerValues) &&
            headerValues.Count == 1)
        {
            var value = headerValues[0];

            if (!string.IsNullOrWhiteSpace(value) && value.Length <= MaxCorrelationIdLength)
            {
                return value;
            }
        }

        return Guid.NewGuid().ToString(Constants.Correlation.IdFormat);
    }
}
