using Microsoft.EntityFrameworkCore;
using NotificationService.Inbox;
using NotificationService.Persistence;

namespace NotificationService.Tests.Inbox;

public sealed class InboxServiceTests
{
    [Fact]
    public async Task IsProcessedAsync_UnknownEvent_ReturnsSuccessfulFalse()
    {
        await using var context = CreateContext();
        var service = new InboxService(context);

        var result = await service.IsProcessedAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccessfull);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task MarkProcessedAsync_ValidEvent_PersistsMessageThatCanBeFound()
    {
        await using var context = CreateContext();
        var service = new InboxService(context);
        var eventId = Guid.NewGuid();
        var receivedAtUtc = new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);
        var processedAtUtc = receivedAtUtc.AddSeconds(2);

        var markResult = await service.MarkProcessedAsync(
            eventId,
            "user.registered",
            1,
            receivedAtUtc,
            processedAtUtc,
            CancellationToken.None);
        var lookupResult = await service.IsProcessedAsync(eventId, CancellationToken.None);
        var persisted = await context.InboxMessages.SingleAsync();

        Assert.True(markResult.IsSuccessfull);
        Assert.True(lookupResult.IsSuccessfull);
        Assert.True(lookupResult.Value);
        Assert.Equal(eventId, persisted.EventId);
        Assert.Equal(receivedAtUtc, persisted.ReceivedAtUtc);
        Assert.Equal(processedAtUtc, persisted.ProcessedAtUtc);
    }

    private static NotificationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new NotificationDbContext(options);
    }
}
