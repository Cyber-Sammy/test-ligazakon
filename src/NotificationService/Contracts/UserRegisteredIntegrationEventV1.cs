namespace NotificationService.Contracts;

public sealed record UserRegisteredIntegrationEventV1(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    int UserId,
    string FirstName,
    string LastName,
    string? MiddleName,
    string Email,
    string PhoneNumber);
