using NotificationService.Contracts;
using NotificationService.Email;
using NotificationService.Handlers;

namespace NotificationService.Tests.Handlers;

public sealed class UserRegisteredEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_CreatesAndSendsRegistrationEmail()
    {
        var emailSender = new StubEmailSender();
        var handler = new UserRegisteredEventHandler(emailSender);
        var integrationEvent = new UserRegisteredIntegrationEventV1(
            Guid.NewGuid(),
            new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero),
            42,
            "Jane",
            "Doe",
            null,
            "jane@example.com",
            "+380501234567");
        using var cancellationSource = new CancellationTokenSource();

        await handler.HandleAsync(integrationEvent, cancellationSource.Token);

        Assert.NotNull(emailSender.Message);
        Assert.Equal("jane@example.com", emailSender.Message.Recipient);
        Assert.Equal("Registration completed", emailSender.Message.Subject);
        Assert.Equal(
            "Hello, Jane! Your registration was successful.",
            emailSender.Message.Body);
        Assert.Equal(cancellationSource.Token, emailSender.CancellationToken);
    }

    private sealed class StubEmailSender : IEmailSender
    {
        public EmailMessage? Message { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task SendAsync(
            EmailMessage message,
            CancellationToken cancellationToken)
        {
            Message = message;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }
}
