using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserService.Application.Interfaces.Infrastructure.Broker;
using UserService.Infrastructure.Common;
using UserService.Infrastructure.Kafka;
using UserService.Infrastructure.Kafka.Options;

namespace UserService.Infrastructure.Extensions;

public static class KafkaConfiguration
{
    public static IServiceCollection ConfigureKafka(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<KafkaOptions>()
            .Bind(configuration.GetRequiredSection(KafkaOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.BootstrapServers),
                InfrastructureConstants.Kafka.BootstrapServersNotConfigured)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Topics.UserEvents),
                InfrastructureConstants.Kafka.UserEventsTopicNotConfigured)
            .Validate(
                options => options.MessageTimeoutMilliseconds > 0,
                InfrastructureConstants.Kafka.MessageTimeoutMustBePositive)
            .ValidateOnStart();

        services.AddSingleton<IProducer<string, string>>(serviceProvider =>
        {
            var kafkaOptions = serviceProvider
                .GetRequiredService<IOptions<KafkaOptions>>()
                .Value;
            var logger = serviceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(InfrastructureConstants.Kafka.ClientId);

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = kafkaOptions.BootstrapServers,
                ClientId = InfrastructureConstants.Kafka.ClientId,
                MessageTimeoutMs = kafkaOptions.MessageTimeoutMilliseconds,
                EnableIdempotence = true,
                Acks = Acks.All
            };

            return new ProducerBuilder<string, string>(producerConfig)
                .SetLogHandler(static (_, _) => { })
                .SetErrorHandler((_, error) =>
                {
                    if (error.IsFatal)
                    {
                        logger.LogCritical(
                            InfrastructureConstants.Kafka.ProducerFatalError,
                            error.Code,
                            error.Reason);
                    }
                })
                .Build();
        });

        services.AddSingleton<IIntegrationEventPublisher, IntegrationEventPublisher>();

        return services;
    }
}
