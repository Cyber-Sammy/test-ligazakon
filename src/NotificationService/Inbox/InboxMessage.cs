using NotificationService.Common;

namespace NotificationService.Inbox;

public sealed class InboxMessage
{
    private InboxMessage() { }

    private InboxMessage(
        Guid eventId,
        string eventType,
        int eventVersion,
        DateTimeOffset receivedAtUtc,
        DateTimeOffset processedAtUtc)
    {
        EventId = eventId;
        EventType = eventType;
        EventVersion = eventVersion;
        ReceivedAtUtc = receivedAtUtc;
        ProcessedAtUtc = processedAtUtc;
    }

    public Guid EventId { get; private set; }

    public string EventType { get; private set; } = null!;

    public int EventVersion { get; private set; }

    public DateTimeOffset ReceivedAtUtc { get; private set; }

    public DateTimeOffset ProcessedAtUtc { get; private set; }

    public static InboxMessage CreateProcessed(
        Guid eventId,
        string eventType,
        int eventVersion,
        DateTimeOffset receivedAtUtc,
        DateTimeOffset processedAtUtc)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException(Constants.Inbox.EmptyEventId, nameof(eventId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentOutOfRangeException.ThrowIfLessThan(eventVersion, 1);
        EnsureUtc(receivedAtUtc, nameof(receivedAtUtc));
        EnsureUtc(processedAtUtc, nameof(processedAtUtc));

        if (processedAtUtc < receivedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(processedAtUtc),
                Constants.Inbox.ProcessingBeforeReceipt);
        }

        return new InboxMessage(
            eventId,
            eventType.Trim(),
            eventVersion,
            receivedAtUtc,
            processedAtUtc);
    }

    private static void EnsureUtc(DateTimeOffset timestamp, string parameterName)
    {
        if (timestamp.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                string.Format(Constants.Inbox.TimestampMustUseUtc, parameterName),
                parameterName);
        }
    }
}
