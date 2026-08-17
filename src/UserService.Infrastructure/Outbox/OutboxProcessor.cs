using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserService.Application.Interfaces.Infrastructure.Broker;
using UserService.Application.Interfaces.UnitOfWork;
using UserService.Application.Models.IntegrationEvents;
using UserService.Infrastructure.Common;
using UserService.Infrastructure.Outbox.Abstractions;
using UserService.Infrastructure.Outbox.Options;

namespace UserService.Infrastructure.Outbox;

public sealed class OutboxProcessor : IOutboxProcessor
{
    public OutboxProcessor(
        IOutboxReader outboxReader,
        IIntegrationEventPublisher publisher,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IOptions<OutboxProcessingOptions> options,
        ILogger<OutboxProcessor> logger)
    {
        _outboxReader = outboxReader;
        _publisher = publisher;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow();

        var messages = await _outboxReader.GetPendingAsync(
            nowUtc,
            _options.BatchSize,
            cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        _logger.LogInformation(
            InfrastructureConstants.Logging.ProcessingOutboxBatch,
            messages.Count);

        foreach (var message in messages)
        {
            var envelope = new IntegrationEventEnvelope(
                message.Id,
                message.Type,
                message.Version,
                message.OccurredAtUtc,
                message.PartitionKey,
                message.Payload);

            try
            {
                await _publisher.PublishAsync(envelope, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation(InfrastructureConstants.Logging.OutboxProcessingCancelled);
                throw;
            }
            catch (Exception exception)
            {
                var nextAttemptAtUtc = _timeProvider
                    .GetUtcNow()
                    .AddSeconds(_options.RetryDelaySeconds);

                _logger.LogWarning(
                    exception,
                    InfrastructureConstants.Logging.OutboxPublishingFailed,
                    message.Id);

                message.RegisterFailure(GetErrorMessage(exception), nextAttemptAtUtc);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    InfrastructureConstants.Logging.OutboxFailureRegistered,
                    message.Id,
                    nextAttemptAtUtc);

                continue;
            }

            message.MarkPublished(_timeProvider.GetUtcNow());
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private static string GetErrorMessage(Exception exception) =>
        string.IsNullOrWhiteSpace(exception.Message)
            ? exception.GetType().Name
            : exception.Message;

    private readonly IOutboxReader _outboxReader;
    private readonly IIntegrationEventPublisher _publisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly OutboxProcessingOptions _options;
    private readonly ILogger<OutboxProcessor> _logger;
}
