namespace UserService.Application.Models.IntegrationEvents;

public sealed record IntegrationEventEnvelope(
    Guid EventId,
    string EventType,
    int Version,
    DateTimeOffset OccurredAtUtc,
    string MessageKey,
    string Payload);
