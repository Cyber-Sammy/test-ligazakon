namespace UserService.Infrastructure.Outbox.Abstractions;

public interface IOutboxReader
{
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(
        DateTimeOffset nowUtc,
        int batchSize,
        CancellationToken cancellationToken);
}
