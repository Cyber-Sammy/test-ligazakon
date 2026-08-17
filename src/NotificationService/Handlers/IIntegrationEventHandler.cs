namespace NotificationService.Handlers;

public interface IIntegrationEventHandler<TEvent>
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
