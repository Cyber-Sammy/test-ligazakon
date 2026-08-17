using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UserService.Application.Interfaces.Infrastructure.Broker;
using UserService.Application.Interfaces.UnitOfWork;
using UserService.Application.Models.IntegrationEvents;
using UserService.Infrastructure.Outbox;
using UserService.Infrastructure.Outbox.Abstractions;
using UserService.Infrastructure.Outbox.Options;

namespace UserService.Tests.Infrastructure;

public sealed class OutboxProcessorTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProcessBatchAsync_EmptyBatch_DoesNotPublishOrSaveChanges()
    {
        var publisher = new StubPublisher
        {
            Publish = (_, _) => throw new InvalidOperationException("Publisher should not be called.")
        };
        var unitOfWork = new StubUnitOfWork();
        var processor = CreateProcessor(new StubOutboxReader(), publisher, unitOfWork);

        await processor.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ProcessBatchAsync_PublishedMessage_MarksItPublishedAndSavesChanges()
    {
        var message = CreateMessage();
        var reader = new StubOutboxReader(message);
        IntegrationEventEnvelope? publishedEnvelope = null;
        var publisher = new StubPublisher
        {
            Publish = (envelope, _) =>
            {
                publishedEnvelope = envelope;
                return Task.CompletedTask;
            }
        };
        var unitOfWork = new StubUnitOfWork();
        var processor = CreateProcessor(reader, publisher, unitOfWork);

        await processor.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(25, reader.ObservedBatchSize);
        Assert.Equal(Now, reader.ObservedNowUtc);
        Assert.NotNull(publishedEnvelope);
        Assert.Equal(message.Id, publishedEnvelope.EventId);
        Assert.Equal(message.PartitionKey, publishedEnvelope.MessageKey);
        Assert.Equal(message.Payload, publishedEnvelope.Payload);
        Assert.Equal(Now, message.PublishedAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ProcessBatchAsync_PublishingFails_RegistersFailureAndSavesChanges()
    {
        var message = CreateMessage();
        var publisher = new StubPublisher
        {
            Publish = (_, _) => Task.FromException(new InvalidOperationException("Kafka unavailable."))
        };
        var unitOfWork = new StubUnitOfWork();
        var processor = CreateProcessor(new StubOutboxReader(message), publisher, unitOfWork);

        await processor.ProcessBatchAsync(CancellationToken.None);

        Assert.Null(message.PublishedAtUtc);
        Assert.Equal(1, message.Attempts);
        Assert.Equal("Kafka unavailable.", message.LastError);
        Assert.Equal(Now.AddSeconds(15), message.NextAttemptAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ProcessBatchAsync_CancelledPublishing_PropagatesWithoutChangingMessage()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var message = CreateMessage();
        var publisher = new StubPublisher
        {
            Publish = (_, token) => Task.FromCanceled(token)
        };
        var unitOfWork = new StubUnitOfWork();
        var processor = CreateProcessor(new StubOutboxReader(message), publisher, unitOfWork);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            processor.ProcessBatchAsync(cancellationSource.Token));

        Assert.Null(message.PublishedAtUtc);
        Assert.Equal(0, message.Attempts);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ProcessBatchAsync_SaveAfterPublishingFails_DoesNotRegisterBrokerFailure()
    {
        var expectedException = new InvalidOperationException("PostgreSQL unavailable.");
        var message = CreateMessage();
        var unitOfWork = new StubUnitOfWork
        {
            SaveChanges = _ => Task.FromException(expectedException)
        };
        var processor = CreateProcessor(
            new StubOutboxReader(message),
            new StubPublisher(),
            unitOfWork);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.ProcessBatchAsync(CancellationToken.None));

        Assert.Same(expectedException, exception);
        Assert.Equal(Now, message.PublishedAtUtc);
        Assert.Equal(0, message.Attempts);
        Assert.Null(message.LastError);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    private static OutboxProcessor CreateProcessor(
        IOutboxReader reader,
        IIntegrationEventPublisher publisher,
        IUnitOfWork unitOfWork) =>
        new(
            reader,
            publisher,
            unitOfWork,
            new FixedTimeProvider(Now),
            Options.Create(new OutboxProcessingOptions
            {
                BatchSize = 25,
                PollingIntervalSeconds = 2,
                RetryDelaySeconds = 15
            }),
            NullLogger<OutboxProcessor>.Instance);

    private static OutboxMessage CreateMessage() =>
        OutboxMessage.Create(
            Guid.NewGuid(),
            "user-42",
            "user.registered",
            1,
            "{\"userId\":42}",
            Now.AddMinutes(-1));

    private sealed class FixedTimeProvider(DateTimeOffset nowUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => nowUtc;
    }

    private sealed class StubOutboxReader(params OutboxMessage[] messages) : IOutboxReader
    {
        public DateTimeOffset? ObservedNowUtc { get; private set; }
        public int? ObservedBatchSize { get; private set; }

        public Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(
            DateTimeOffset nowUtc,
            int batchSize,
            CancellationToken cancellationToken)
        {
            ObservedNowUtc = nowUtc;
            ObservedBatchSize = batchSize;
            return Task.FromResult<IReadOnlyList<OutboxMessage>>(messages);
        }
    }

    private sealed class StubPublisher : IIntegrationEventPublisher
    {
        public Func<IntegrationEventEnvelope, CancellationToken, Task>? Publish { get; init; }

        public Task PublishAsync(
            IntegrationEventEnvelope integrationEvent,
            CancellationToken cancellationToken) =>
            Publish?.Invoke(integrationEvent, cancellationToken) ?? Task.CompletedTask;
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public Func<CancellationToken, Task>? SaveChanges { get; init; }
        public int SaveChangesCallCount { get; private set; }

        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("A transaction is not expected during outbox publishing.");

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return SaveChanges?.Invoke(cancellationToken) ?? Task.CompletedTask;
        }
    }
}
