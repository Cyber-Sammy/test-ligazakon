using System.Globalization;
using UserService.Application.Common;

namespace UserService.Application.Interfaces.IntegrationEvents;

public sealed record UserRegisteredIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    int UserId,
    string FirstName,
    string LastName,
    string? MiddleName,
    string Email,
    string PhoneNumber) : IIntegrationEvent
{
    public string EventType => Constants.IntegrationEvents.UserRegisteredType;

    public int Version => Constants.IntegrationEvents.UserRegisteredVersion;

    public string PartitionKey => UserId.ToString(CultureInfo.InvariantCulture);
}
