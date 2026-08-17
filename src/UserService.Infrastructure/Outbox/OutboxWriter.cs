using Microsoft.Extensions.Logging;
using UserService.Application.Interfaces.Infrastructure;
using UserService.Application.Interfaces.IntegrationEvents;
using UserService.Infrastructure.Common;
using UserService.Infrastructure.Contexts;

namespace UserService.Infrastructure.Outbox;

public sealed class OutboxWriter : IOutboxWriter
{
    public OutboxWriter(
        UsersDbContext usersDbContext,
        ILogger<OutboxWriter> logger)
    {
        _usersDbContext = usersDbContext;
        _logger = logger;
    }

    public void Add(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        _logger.LogInformation(
            InfrastructureConstants.Logging.AddingIntegrationEvent,
            integrationEvent.EventId,
            integrationEvent.EventType);

        var outboxMessage = OutboxMessage.Create(integrationEvent);

        _usersDbContext.OutboxMessages.Add(outboxMessage);

        _logger.LogInformation(
            InfrastructureConstants.Logging.IntegrationEventAdded,
            integrationEvent.EventId,
            integrationEvent.EventType);
    }

    private readonly UsersDbContext _usersDbContext;
    private readonly ILogger<OutboxWriter> _logger;
}
