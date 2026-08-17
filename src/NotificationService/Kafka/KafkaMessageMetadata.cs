namespace NotificationService.Kafka;

public sealed record KafkaMessageMetadata(
    Guid EventId,
    string EventType,
    int EventVersion,
    DateTimeOffset OccurredAtUtc);
