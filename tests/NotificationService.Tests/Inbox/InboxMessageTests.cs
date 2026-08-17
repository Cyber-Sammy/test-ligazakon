using NotificationService.Inbox;

namespace NotificationService.Tests.Inbox;

public sealed class InboxMessageTests
{
    [Fact]
    public void CreateProcessed_ValidValues_CreatesMessageAndTrimsEventType()
    {
        var eventId = Guid.NewGuid();
        var receivedAtUtc = Utc(12, 0);
        var processedAtUtc = Utc(12, 1);

        var message = InboxMessage.CreateProcessed(
            eventId,
            "  user.registered  ",
            1,
            receivedAtUtc,
            processedAtUtc);

        Assert.Equal(eventId, message.EventId);
        Assert.Equal("user.registered", message.EventType);
        Assert.Equal(1, message.EventVersion);
        Assert.Equal(receivedAtUtc, message.ReceivedAtUtc);
        Assert.Equal(processedAtUtc, message.ProcessedAtUtc);
    }

    [Fact]
    public void CreateProcessed_EmptyEventId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            InboxMessage.CreateProcessed(
                Guid.Empty,
                "user.registered",
                1,
                Utc(12, 0),
                Utc(12, 1)));

        Assert.Equal("eventId", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateProcessed_NonPositiveVersion_ThrowsArgumentOutOfRangeException(int version)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            InboxMessage.CreateProcessed(
                Guid.NewGuid(),
                "user.registered",
                version,
                Utc(12, 0),
                Utc(12, 1)));

        Assert.Equal("eventVersion", exception.ParamName);
    }

    [Fact]
    public void CreateProcessed_NonUtcTimestamp_ThrowsArgumentException()
    {
        var nonUtc = new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.FromHours(3));

        var exception = Assert.Throws<ArgumentException>(() =>
            InboxMessage.CreateProcessed(
                Guid.NewGuid(),
                "user.registered",
                1,
                nonUtc,
                Utc(12, 1)));

        Assert.Equal("receivedAtUtc", exception.ParamName);
    }

    [Fact]
    public void CreateProcessed_ProcessedBeforeReceived_ThrowsArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            InboxMessage.CreateProcessed(
                Guid.NewGuid(),
                "user.registered",
                1,
                Utc(12, 1),
                Utc(12, 0)));

        Assert.Equal("processedAtUtc", exception.ParamName);
    }

    private static DateTimeOffset Utc(int hour, int minute) =>
        new(2026, 8, 17, hour, minute, 0, TimeSpan.Zero);
}
