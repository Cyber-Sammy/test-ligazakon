using UserService.Infrastructure.Outbox;

namespace UserService.Tests.Infrastructure;

public sealed class OutboxMessageTests
{
    private static readonly DateTimeOffset OccurredAtUtc =
        new(2026, 8, 16, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidValues_CreatesPendingMessage()
    {
        var id = Guid.NewGuid();

        var message = OutboxMessage.Create(
            id,
            "  42  ",
            "  user.registered  ",
            1,
            "{\"userId\":42}",
            OccurredAtUtc);

        Assert.Equal(id, message.Id);
        Assert.Equal("42", message.PartitionKey);
        Assert.Equal("user.registered", message.Type);
        Assert.Equal(1, message.Version);
        Assert.Equal("{\"userId\":42}", message.Payload);
        Assert.Equal(OccurredAtUtc, message.OccurredAtUtc);
        Assert.Null(message.PublishedAtUtc);
        Assert.Equal(0, message.Attempts);
        Assert.Null(message.NextAttemptAtUtc);
        Assert.Null(message.LastError);
    }

    [Fact]
    public void Create_EmptyId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => OutboxMessage.Create(
            Guid.Empty,
            "42",
            "user.registered",
            1,
            "{}",
            OccurredAtUtc));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankPartitionKey_ThrowsArgumentException(string? partitionKey)
    {
        Assert.ThrowsAny<ArgumentException>(() => OutboxMessage.Create(
            Guid.NewGuid(),
            partitionKey!,
            "user.registered",
            1,
            "{}",
            OccurredAtUtc));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankType_ThrowsArgumentException(string? type)
    {
        Assert.ThrowsAny<ArgumentException>(() => OutboxMessage.Create(
            Guid.NewGuid(),
            "42",
            type!,
            1,
            "{}",
            OccurredAtUtc));
    }

    [Fact]
    public void Create_NonPositiveVersion_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OutboxMessage.Create(
            Guid.NewGuid(),
            "42",
            "user.registered",
            0,
            "{}",
            OccurredAtUtc));
    }

    [Fact]
    public void Create_NonUtcOccurrence_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => OutboxMessage.Create(
            Guid.NewGuid(),
            "42",
            "user.registered",
            1,
            "{}",
            OccurredAtUtc.ToOffset(TimeSpan.FromHours(3))));
    }

    [Fact]
    public void RegisterFailure_UpdatesRetryStateAndBoundsErrorLength()
    {
        var message = CreateMessage();
        var nextAttemptAtUtc = OccurredAtUtc.AddMinutes(1);

        message.RegisterFailure(
            $"  {new string('x', 2100)}  ",
            nextAttemptAtUtc);

        Assert.Equal(1, message.Attempts);
        Assert.Equal(nextAttemptAtUtc, message.NextAttemptAtUtc);
        Assert.Equal(2000, message.LastError?.Length);
    }

    [Fact]
    public void RegisterFailure_Twice_IncrementsAttempts()
    {
        var message = CreateMessage();

        message.RegisterFailure("First failure.", OccurredAtUtc.AddMinutes(1));
        message.RegisterFailure("Second failure.", OccurredAtUtc.AddMinutes(2));

        Assert.Equal(2, message.Attempts);
        Assert.Equal("Second failure.", message.LastError);
        Assert.Equal(OccurredAtUtc.AddMinutes(2), message.NextAttemptAtUtc);
    }

    [Fact]
    public void MarkPublished_ClearsRetryStateAndPreservesFirstPublishedTime()
    {
        var message = CreateMessage();
        var publishedAtUtc = OccurredAtUtc.AddMinutes(2);
        message.RegisterFailure("Temporary failure.", OccurredAtUtc.AddMinutes(1));

        message.MarkPublished(publishedAtUtc);
        message.MarkPublished(publishedAtUtc.AddMinutes(1));

        Assert.Equal(publishedAtUtc, message.PublishedAtUtc);
        Assert.Equal(1, message.Attempts);
        Assert.Null(message.NextAttemptAtUtc);
        Assert.Null(message.LastError);
    }

    [Fact]
    public void RegisterFailure_PublishedMessage_ThrowsInvalidOperationException()
    {
        var message = CreateMessage();
        message.MarkPublished(OccurredAtUtc.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() =>
            message.RegisterFailure("Failure.", OccurredAtUtc.AddMinutes(2)));
    }

    [Fact]
    public void StateTimestamp_BeforeOccurrence_ThrowsArgumentOutOfRangeException()
    {
        var message = CreateMessage();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            message.MarkPublished(OccurredAtUtc.AddTicks(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            message.RegisterFailure("Failure.", OccurredAtUtc.AddTicks(-1)));
    }

    private static OutboxMessage CreateMessage() =>
        OutboxMessage.Create(
            Guid.NewGuid(),
            "42",
            "user.registered",
            1,
            "{\"userId\":42}",
            OccurredAtUtc);
}
