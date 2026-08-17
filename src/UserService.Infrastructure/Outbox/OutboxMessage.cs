using UserService.Application.Interfaces.IntegrationEvents;
using UserService.Infrastructure.Common;
using UserService.Infrastructure.Extensions.Serializers;

namespace UserService.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage() { }

    private OutboxMessage(
        Guid id,
        string partitionKey,
        string type,
        int version,
        string payload,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        PartitionKey = partitionKey;
        Type = type;
        Version = version;
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; } = null!;

    public int Version { get; private set; }

    public string Payload { get; private set; } = null!;

    public string PartitionKey { get; private set; } = null!;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset? NextAttemptAtUtc { get; private set; }

    public string? LastError { get; private set; }

    public static OutboxMessage Create(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return Create(
            integrationEvent.EventId,
            integrationEvent.PartitionKey,
            integrationEvent.EventType,
            integrationEvent.Version,
            integrationEvent.Serialize(),
            integrationEvent.OccurredAtUtc);
    }

    public static OutboxMessage Create(
        Guid id,
        string partitionKey,
        string type,
        int version,
        string payload,
        DateTimeOffset occurredAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(InfrastructureConstants.Outbox.EmptyId, nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentOutOfRangeException.ThrowIfLessThan(version, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));

        return new OutboxMessage(
            id,
            partitionKey.Trim(),
            type.Trim(),
            version,
            payload,
            occurredAtUtc);
    }

    public void MarkPublished(DateTimeOffset publishedAtUtc)
    {
        EnsureUtc(publishedAtUtc, nameof(publishedAtUtc));
        EnsureNotBeforeOccurrence(publishedAtUtc, nameof(publishedAtUtc));

        if (PublishedAtUtc is not null)
        {
            return;
        }

        PublishedAtUtc = publishedAtUtc;
        NextAttemptAtUtc = null;
        LastError = null;
    }

    public void RegisterFailure(string error, DateTimeOffset nextAttemptAtUtc)
    {
        if (PublishedAtUtc is not null)
        {
            throw new InvalidOperationException(
                InfrastructureConstants.Outbox.PublishedMessageFailure);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        EnsureUtc(nextAttemptAtUtc, nameof(nextAttemptAtUtc));
        EnsureNotBeforeOccurrence(nextAttemptAtUtc, nameof(nextAttemptAtUtc));

        var normalizedError = error.Trim();

        Attempts = checked(Attempts + 1);
        NextAttemptAtUtc = nextAttemptAtUtc;
        LastError = normalizedError.Length <= InfrastructureConstants.Outbox.LastErrorMaxLength
            ? normalizedError
            : normalizedError[..InfrastructureConstants.Outbox.LastErrorMaxLength];
    }

    private static void EnsureUtc(DateTimeOffset timestamp, string parameterName)
    {
        if (timestamp.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                string.Format(InfrastructureConstants.Outbox.TimestampMustBeUtc, parameterName),
                parameterName);
        }
    }

    private void EnsureNotBeforeOccurrence(
        DateTimeOffset timestamp,
        string parameterName)
    {
        if (timestamp < OccurredAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                string.Format(InfrastructureConstants.Outbox.TimestampBeforeOccurrence, parameterName));
        }
    }
}
