using Confluent.Kafka;
using Microsoft.Extensions.Options;
using NotificationService.Common;
using NotificationService.Contracts;
using NotificationService.Handlers;
using NotificationService.Inbox;
using NotificationService.Kafka;
using NotificationService.Options;
using NotificationService.Serialization;

namespace NotificationService.Consumers;

public sealed class KafkaConsumerWorker(
    IConsumer<string, string> consumer,
    IOptions<KafkaConsumerOptions> options,
    TimeProvider timeProvider,
    ILogger<KafkaConsumerWorker> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var topic = options.Value.Topics.UserEvents;

        consumer.Subscribe(topic);
        logger.LogInformation(Constants.Logging.ConsumerSubscribed, topic, timeProvider.GetUtcNow());

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumed = consumer.Consume(stoppingToken);
                var receivedAtUtc = timeProvider.GetUtcNow();

                await ProcessMessageAsync(consumed, receivedAtUtc, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal hosted-service shutdown.
        }
        finally
        {
            consumer.Close();
            logger.LogInformation(Constants.Logging.ConsumerStopped);
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> consumed, DateTimeOffset receivedAtUtc, CancellationToken cancellationToken)
    {
        var metadataResult = KafkaMessageHeaderParser.Parse(consumed.Message.Headers);

        if (metadataResult.IsFailure)
        {
            logger.LogWarning(Constants.Logging.InvalidKafkaHeaders, consumed.TopicPartitionOffset, metadataResult.Message);
            consumer.Commit(consumed);

            return;
        }

        var metadata = metadataResult.Value;

        if (metadata.EventType != Constants.IntegrationEvents.UserRegisteredType
            || metadata.EventVersion != Constants.IntegrationEvents.UserRegisteredVersion)
        {
            logger.LogWarning(Constants.Logging.UnsupportedIntegrationEvent,
                metadata.EventId,
                metadata.EventType,
                metadata.EventVersion);
            consumer.Commit(consumed);

            return;
        }

        var eventResult = IntegrationEventDeserializer.Deserialize<UserRegisteredIntegrationEventV1>(consumed.Message.Value);

        if (eventResult.IsFailure)
        {
            logger.LogWarning(Constants.Logging.InvalidKafkaPayload,
                metadata.EventId,
                eventResult.Message);
            consumer.Commit(consumed);

            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ProcessEventAsync(eventResult.Value, metadata, receivedAtUtc, cancellationToken);

                consumer.Commit(consumed);

                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, Constants.Logging.IntegrationEventProcessingFailed,
                    metadata.EventId,
                    consumed.TopicPartitionOffset,
                    options.Value.ProcessingRetryDelaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(options.Value.ProcessingRetryDelaySeconds),
                    cancellationToken);
            }
        }
    }

    private async Task ProcessEventAsync(UserRegisteredIntegrationEventV1 integrationEvent, KafkaMessageMetadata metadata,
        DateTimeOffset receivedAtUtc, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var inboxService = scope.ServiceProvider.GetRequiredService<IInboxService>();
        var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<UserRegisteredIntegrationEventV1>>();

        var inboxResult = await inboxService.IsProcessedAsync(metadata.EventId, cancellationToken);

        if (inboxResult.IsFailure)
        {
            throw new InvalidOperationException(inboxResult.Message);
        }

        if (inboxResult.Value)
        {
            logger.LogInformation(Constants.Logging.IntegrationEventAlreadyProcessed, metadata.EventId);

            return;
        }

        await handler.HandleAsync(integrationEvent, cancellationToken);

        var markResult = await inboxService.MarkProcessedAsync(
            metadata.EventId,
            metadata.EventType,
            metadata.EventVersion,
            receivedAtUtc,
            timeProvider.GetUtcNow(),
            cancellationToken);

        if (markResult.IsFailure)
        {
            throw new InvalidOperationException(markResult.Message);
        }
    }
}
