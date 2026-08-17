namespace UserService.Application.Interfaces.IntegrationEvents;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAtUtc { get; }

    string PartitionKey { get; }

    string EventType { get; }

    int Version { get; }
}
