using NotificationService.Common.Results;
using NotificationService.Contracts;
using NotificationService.Serialization;

namespace NotificationService.Tests.Serialization;

public sealed class IntegrationEventDeserializerTests
{
    [Fact]
    public void Deserialize_ValidPayload_ReturnsEvent()
    {
        const string payload = """
            {
              "eventId": "34cb87e4-21c2-41c2-9d1e-1a1ef2243716",
              "occurredAtUtc": "2026-08-17T12:00:00+00:00",
              "userId": 42,
              "firstName": "Jane",
              "lastName": "Doe",
              "middleName": null,
              "email": "jane@example.com",
              "phoneNumber": "+380501234567"
            }
            """;

        var result = IntegrationEventDeserializer
            .Deserialize<UserRegisteredIntegrationEventV1>(payload);

        Assert.True(result.IsSuccessfull);
        Assert.Equal(42, result.Value.UserId);
        Assert.Equal("jane@example.com", result.Value.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deserialize_EmptyPayload_ReturnsValidationError(string? payload)
    {
        var result = IntegrationEventDeserializer
            .Deserialize<UserRegisteredIntegrationEventV1>(payload);

        Assert.Equal(ResultStatus.ValidationError, result.Status);
    }

    [Fact]
    public void Deserialize_InvalidJson_ReturnsValidationError()
    {
        var result = IntegrationEventDeserializer
            .Deserialize<UserRegisteredIntegrationEventV1>("{ invalid json }");

        Assert.Equal(ResultStatus.ValidationError, result.Status);
    }

    [Fact]
    public void Deserialize_MissingRequiredConstructorProperties_ReturnsValidationError()
    {
        var result = IntegrationEventDeserializer
            .Deserialize<UserRegisteredIntegrationEventV1>("{}");

        Assert.Equal(ResultStatus.ValidationError, result.Status);
    }
}
