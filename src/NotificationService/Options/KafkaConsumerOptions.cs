namespace NotificationService.Options;

public sealed class KafkaConsumerOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } = string.Empty;

    public string GroupId { get; init; } = string.Empty;

    public int ProcessingRetryDelaySeconds { get; init; }

    public KafkaTopicsOptions Topics { get; init; } = new();
}

public sealed class KafkaTopicsOptions
{
    public string UserEvents { get; init; } = string.Empty;
}
