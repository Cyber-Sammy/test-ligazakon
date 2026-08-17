namespace UserService.Infrastructure.Outbox.Abstractions;

public interface IOutboxProcessor
{
    Task ProcessBatchAsync(CancellationToken cancellationToken);
}
