using Serilog;
using UserService.Api.Middleware;
using UserService.Application.Interfaces.Services;

namespace UserService.Api.Extensions;

public static class RegisterDependencies
{
    public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddExceptionHandler<ExceptionHandlingMiddleware>();
        services.AddProblemDetails();
        services.AddScoped<IUserService, Application.Services.UserService>();

        return services;
    }

    public static WebApplication UseApplicationMiddlewares(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();

        return app;
    }

    public static IHostBuilder AddSerilog(this IHostBuilder host, IServiceCollection services)
    {
        host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services));

        return host;
    }
}
