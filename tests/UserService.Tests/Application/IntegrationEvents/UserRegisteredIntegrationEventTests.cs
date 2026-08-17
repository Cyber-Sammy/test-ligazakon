using UserService.Application.Common;
using UserService.Application.Interfaces.IntegrationEvents;

namespace UserService.Tests.Application.IntegrationEvents;

public sealed class UserRegisteredIntegrationEventTests
{
    [Fact]
    public void Constructor_CreatesVersionedIntegrationEvent()
    {
        var eventId = Guid.NewGuid();
        var occurredAtUtc = new DateTimeOffset(
            2026,
            8,
            14,
            12,
            30,
            0,
            TimeSpan.Zero);

        IIntegrationEvent integrationEvent = new UserRegisteredIntegrationEvent(
            eventId,
            occurredAtUtc,
            42,
            "Jane",
            "Doe",
            "Marie",
            "jane@example.com",
            "+380501234567");

        var userRegistered = Assert.IsType<UserRegisteredIntegrationEvent>(
            integrationEvent);
        Assert.Equal(eventId, userRegistered.EventId);
        Assert.Equal(occurredAtUtc, userRegistered.OccurredAtUtc);
        Assert.Equal(42, userRegistered.UserId);
        Assert.Equal("Jane", userRegistered.FirstName);
        Assert.Equal("Doe", userRegistered.LastName);
        Assert.Equal("Marie", userRegistered.MiddleName);
        Assert.Equal("jane@example.com", userRegistered.Email);
        Assert.Equal("+380501234567", userRegistered.PhoneNumber);
        Assert.Equal("42", userRegistered.PartitionKey);
        Assert.Equal(
            Constants.IntegrationEvents.UserRegisteredType,
            userRegistered.EventType);
        Assert.Equal(
            Constants.IntegrationEvents.UserRegisteredVersion,
            userRegistered.Version);
    }
}
