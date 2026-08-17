using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using UserService.Infrastructure.Common;
using UserService.Infrastructure.Outbox;
using UserService.Infrastructure.Outbox.Abstractions;
using UserService.Infrastructure.Outbox.Jobs;
using UserService.Infrastructure.Outbox.Options;

namespace UserService.Infrastructure.Extensions;

public static class OutboxProcessingConfiguration
{
    public static IServiceCollection AddOutboxProcessing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(OutboxProcessingOptions.SectionName);
        var configurationOptions = section.Get<OutboxProcessingOptions>()
            ?? throw new InvalidOperationException(InfrastructureConstants.Outbox.ConfigurationNotFound);

        Validate(configurationOptions);

        services
            .AddOptions<OutboxProcessingOptions>()
            .Bind(section)
            .Validate(options => options.BatchSize > 0, InfrastructureConstants.Outbox.BatchSizeMustBePositive)
            .Validate(
                options => options.PollingIntervalSeconds > 0,
                InfrastructureConstants.Outbox.PollingIntervalMustBePositive)
            .Validate(
                options => options.RetryDelaySeconds > 0,
                InfrastructureConstants.Outbox.RetryDelayMustBePositive)
            .ValidateOnStart();

        var jobKey = new JobKey(InfrastructureConstants.Outbox.PublisherJob);

        services.AddScoped<IOutboxReader, OutboxReader>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();

        if (!configurationOptions.Enabled)
        {
            return services;
        }

        services.AddQuartz(quartz =>
        {
            quartz.AddJob<OutboxPublisherJob>(options => options.WithIdentity(jobKey));

            quartz.AddTrigger(options => options
                .ForJob(jobKey)
                .WithIdentity(InfrastructureConstants.Outbox.PublisherTrigger)
                .StartNow()
                .WithSimpleSchedule(schedule => schedule
                    .WithIntervalInSeconds(configurationOptions.PollingIntervalSeconds)
                    .RepeatForever()));
        });

        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        return services;
    }

    private static void Validate(OutboxProcessingOptions options)
    {
        if (options.BatchSize <= 0)
        {
            throw new InvalidOperationException(InfrastructureConstants.Outbox.BatchSizeMustBePositive);
        }

        if (options.PollingIntervalSeconds <= 0)
        {
            throw new InvalidOperationException(InfrastructureConstants.Outbox.PollingIntervalMustBePositive);
        }

        if (options.RetryDelaySeconds <= 0)
        {
            throw new InvalidOperationException(InfrastructureConstants.Outbox.RetryDelayMustBePositive);
        }
    }
}
