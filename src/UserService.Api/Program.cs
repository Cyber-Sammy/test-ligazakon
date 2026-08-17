using Serilog;
using Serilog.Events;
using Scalar.AspNetCore;
using UserService.Api.Extensions;
using UserService.Application.Common;
using UserService.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilog(builder.Services);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddApplicationDependencies();
builder.Services.AddInfrastructureDependencies(builder.Configuration);
builder.Services.AddOutboxProcessing(builder.Configuration);

var app = builder.Build();

app.UseApplicationMiddlewares();
app.UseSerilogRequestLogging(options =>
    options.GetLevel = GetRequestLogLevel);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();
app.MapGet(
    Constants.Routes.Health,
    () => Results.Ok(new { status = Constants.Health.HealthyStatus }));

app.Run();

static LogEventLevel GetRequestLogLevel(
    HttpContext context,
    double _,
    Exception? exception)
{
    if (exception is not null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
    {
        return LogEventLevel.Error;
    }

    var path = context.Request.Path;

    return path.StartsWithSegments(Constants.Routes.Health)
           || path.StartsWithSegments(Constants.Routes.ScalarPrefix)
           || path.StartsWithSegments(Constants.Routes.OpenApiPrefix)
        ? LogEventLevel.Debug
        : LogEventLevel.Information;
}

public partial class Program;
