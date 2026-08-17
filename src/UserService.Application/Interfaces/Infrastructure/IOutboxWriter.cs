using UserService.Application.Interfaces.IntegrationEvents;

namespace UserService.Application.Interfaces.Infrastructure;

public interface IOutboxWriter
{
    void Add(IIntegrationEvent integrationEvent);
}
