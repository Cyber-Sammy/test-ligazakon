using Microsoft.EntityFrameworkCore;
using NotificationService.Common.Results;
using NotificationService.Persistence;

namespace NotificationService.Inbox;

public sealed class InboxService(NotificationDbContext dbContext) : IInboxService
{
    public async Task<Result<bool>> IsProcessedAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var isProcessed = await dbContext.InboxMessages.AnyAsync(message => message.EventId == eventId, cancellationToken);

        return Result<bool>.Success(isProcessed);
    }

    public async Task<Result> MarkProcessedAsync(
        Guid eventId,
        string eventType,
        int eventVersion,
        DateTimeOffset receivedAtUtc,
        DateTimeOffset processedAtUtc,
        CancellationToken cancellationToken)
    {
        var inboxMessage = InboxMessage.CreateProcessed(
            eventId,
            eventType,
            eventVersion,
            receivedAtUtc,
            processedAtUtc);

        dbContext.InboxMessages.Add(inboxMessage);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
