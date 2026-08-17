using UserService.Application.Models.IntegrationEvents;

namespace UserService.Application.Interfaces.Infrastructure.Broker;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(IntegrationEventEnvelope integrationEvent, CancellationToken cancellationToken);
}
