namespace UserService.Infrastructure.Outbox.Options;

public sealed class OutboxProcessingOptions
{
    public const string SectionName = "OutboxProcessing";

    public bool Enabled { get; init; }

    public int BatchSize { get; init; }

    public int PollingIntervalSeconds { get; init; }

    public int RetryDelaySeconds { get; init; }
}
