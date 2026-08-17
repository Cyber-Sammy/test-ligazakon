namespace UserService.Infrastructure.Kafka.Options;
public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } = string.Empty;

    public int MessageTimeoutMilliseconds { get; init; }

    public KafkaTopicsOptions Topics { get; init; } = new();
}

public sealed class KafkaTopicsOptions
{
    public string UserEvents { get; init; } = string.Empty;
}
