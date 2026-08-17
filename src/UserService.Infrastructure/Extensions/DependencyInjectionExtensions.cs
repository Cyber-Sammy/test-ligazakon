using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Interfaces.Infrastructure;
using UserService.Application.Interfaces.UnitOfWork;
using UserService.Infrastructure.Common;
using UserService.Infrastructure.Contexts;
using UserService.Infrastructure.Outbox;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.UnitOfWork;

namespace UserService.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructureDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(InfrastructureConstants.DefaultConnectionName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                string.Format(InfrastructureConstants.ConnectionStringNotConfigured, InfrastructureConstants.DefaultConnectionName));
        }

        services.AddDbContext<UsersDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();

        services.ConfigureKafka(configuration);

        return services;
    }
}
