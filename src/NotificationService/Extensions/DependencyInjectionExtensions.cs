using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotificationService.Common;
using NotificationService.Consumers;
using NotificationService.Contracts;
using NotificationService.Email;
using NotificationService.Handlers;
using NotificationService.Inbox;
using NotificationService.Options;
using NotificationService.Persistence;

namespace NotificationService.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddNotificationDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ConfigureOptions(services, configuration);

        var connectionString = configuration.GetConnectionString(
            Constants.Persistence.DefaultConnectionName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                string.Format(Constants.Persistence.ConnectionStringNotConfigured, Constants.Persistence.DefaultConnectionName));
        }

        services.AddDbContext<NotificationDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<TimeProvider>(TimeProvider.System);

        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<KafkaConsumerOptions>>().Value;
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(Constants.Kafka.ClientId);

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = options.BootstrapServers,
                GroupId = options.GroupId,
                ClientId = Constants.Kafka.ClientId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = false,
                AllowAutoCreateTopics = false
            };

            return new ConsumerBuilder<string, string>(consumerConfig)
                .SetLogHandler(static (_, _) => { })
                .SetErrorHandler((_, error) =>
                {
                    if (error.IsFatal)
                    {
                        logger.LogCritical(
                            Constants.Kafka.FatalError,
                            error.Code,
                            error.Reason);
                    }
                })
                .Build();
        });

        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IInboxService, InboxService>();
        services.AddScoped<IIntegrationEventHandler<UserRegisteredIntegrationEventV1>, UserRegisteredEventHandler>();
        services.AddHostedService<KafkaConsumerWorker>();

        return services;
    }

    private static void ConfigureOptions(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<KafkaConsumerOptions>()
            .Bind(configuration.GetRequiredSection(KafkaConsumerOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.BootstrapServers),
                Constants.Kafka.BootstrapServersNotConfigured)
            .Validate(options => !string.IsNullOrWhiteSpace(options.GroupId),
                Constants.Kafka.GroupIdNotConfigured)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Topics.UserEvents),
                Constants.Kafka.UserEventsTopicNotConfigured)
            .Validate(options => options.ProcessingRetryDelaySeconds > 0,
                Constants.Kafka.ProcessingRetryDelayMustBePositive)
            .ValidateOnStart();

        services
            .AddOptions<SmtpOptions>()
            .Bind(configuration.GetRequiredSection(SmtpOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Host), Constants.Smtp.HostNotConfigured)
            .Validate(options => options.Port is > 0 and <= 65535, Constants.Smtp.PortOutOfRange)
            .Validate(options => !string.IsNullOrWhiteSpace(options.SenderName),
                Constants.Smtp.SenderNameNotConfigured)
            .Validate(options => !string.IsNullOrWhiteSpace(options.SenderAddress),
                Constants.Smtp.SenderAddressNotConfigured)
            .ValidateOnStart();
    }
}
