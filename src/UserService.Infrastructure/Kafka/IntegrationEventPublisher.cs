using System.Globalization;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserService.Application.Interfaces.Infrastructure.Broker;
using UserService.Application.Models.IntegrationEvents;
using UserService.Infrastructure.Common;
using UserService.Infrastructure.Kafka.Options;

namespace UserService.Infrastructure.Kafka;

public sealed class IntegrationEventPublisher : IIntegrationEventPublisher
{
    public IntegrationEventPublisher(
        IProducer<string, string> producer,
        IOptions<KafkaOptions> options,
        ILogger<IntegrationEventPublisher> logger)
    {
        _producer = producer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(
        IntegrationEventEnvelope integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        _logger.LogInformation(InfrastructureConstants.Logging.PublishingIntegrationEvent,
            integrationEvent.EventId,
            integrationEvent.EventType);

        var message = new Message<string, string>
        {
            Key = integrationEvent.MessageKey,
            Value = integrationEvent.Payload,
            Headers = CreateHeaders(integrationEvent)
        };

        await _producer.ProduceAsync(_options.Topics.UserEvents, message, cancellationToken);

        _logger.LogInformation(InfrastructureConstants.Logging.IntegrationEventPublished,
            integrationEvent.EventId,
            integrationEvent.EventType);
    }

    private static Headers CreateHeaders(IntegrationEventEnvelope integrationEvent) =>
    [
        new Header(
            InfrastructureConstants.Kafka.Headers.EventId,
            Encode(integrationEvent.EventId.ToString(InfrastructureConstants.Kafka.EventIdFormat))),
        new Header(
            InfrastructureConstants.Kafka.Headers.EventType,
            Encode(integrationEvent.EventType)),
        new Header(
            InfrastructureConstants.Kafka.Headers.EventVersion,
            Encode(integrationEvent.Version.ToString(CultureInfo.InvariantCulture))),
        new Header(
            InfrastructureConstants.Kafka.Headers.OccurredAtUtc,
            Encode(integrationEvent.OccurredAtUtc.ToString(
                InfrastructureConstants.Kafka.TimestampFormat,
                CultureInfo.InvariantCulture))),
        new Header(
            InfrastructureConstants.Kafka.Headers.ContentType,
            Encode(InfrastructureConstants.Kafka.JsonContentType))
    ];

    private static byte[] Encode(string value) => Encoding.UTF8.GetBytes(value);

    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<IntegrationEventPublisher> _logger;
}
