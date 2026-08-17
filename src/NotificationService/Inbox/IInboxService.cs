using NotificationService.Common.Results;

namespace NotificationService.Inbox;

public interface IInboxService
{
    Task<Result<bool>> IsProcessedAsync(
        Guid eventId,
        CancellationToken cancellationToken);

    Task<Result> MarkProcessedAsync(
        Guid eventId,
        string eventType,
        int eventVersion,
        DateTimeOffset receivedAtUtc,
        DateTimeOffset processedAtUtc,
        CancellationToken cancellationToken);
}
