using System.Text.Json;
using NotificationService.Common;
using NotificationService.Common.Results;

namespace NotificationService.Serialization;

public static class IntegrationEventDeserializer
{
    public static Result<TEvent> Deserialize<TEvent>(string? payload)
        where TEvent : class
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return Result<TEvent>.Failure(
                ResultStatus.ValidationError,
                Constants.Kafka.EmptyPayload);
        }

        try
        {
            var integrationEvent = JsonSerializer.Deserialize<TEvent>(
                payload,
                SerializerOptions);

            return integrationEvent is null
                ? InvalidPayload<TEvent>()
                : Result<TEvent>.Success(integrationEvent);
        }
        catch (JsonException)
        {
            return InvalidPayload<TEvent>();
        }
    }

    private static Result<TEvent> InvalidPayload<TEvent>()
        where TEvent : class =>
        Result<TEvent>.Failure(
            ResultStatus.ValidationError,
            string.Format(Constants.Kafka.InvalidPayload, typeof(TEvent).Name));

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        RespectRequiredConstructorParameters = true
    };
}
