using System.Text.Json;
using UserService.Application.Interfaces.IntegrationEvents;

namespace UserService.Infrastructure.Extensions.Serializers;

public static class IntegrationEventSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Serialize(this IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), SerializerOptions);
    }
}