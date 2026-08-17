using NotificationService.Common;
using NotificationService.Contracts;
using NotificationService.Email;

namespace NotificationService.Handlers;

public sealed class UserRegisteredEventHandler(IEmailSender emailSender)
    : IIntegrationEventHandler<UserRegisteredIntegrationEventV1>
{
    public async Task HandleAsync(
        UserRegisteredIntegrationEventV1 integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var emailMessage = new EmailMessage(
            integrationEvent.Email,
            Constants.Email.UserRegisteredSubject,
            string.Format(
                Constants.Email.UserRegisteredBody,
                integrationEvent.FirstName));

        await emailSender.SendAsync(emailMessage, cancellationToken);
    }
}
