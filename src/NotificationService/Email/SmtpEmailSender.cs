using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NotificationService.Options;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace NotificationService.Email;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    public async Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var smtpOptions = options.Value;
        var mimeMessage = new MimeMessage
        {
            Subject = message.Subject,
            Body = new TextPart("plain")
            {
                Text = message.Body
            }
        };

        mimeMessage.From.Add(new MailboxAddress(
            smtpOptions.SenderName,
            smtpOptions.SenderAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(message.Recipient));

        using var smtpClient = new SmtpClient();

        await smtpClient.ConnectAsync(
            smtpOptions.Host,
            smtpOptions.Port,
            smtpOptions.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.None,
            cancellationToken);

        try
        {
            await smtpClient.SendAsync(mimeMessage, cancellationToken);
        }
        finally
        {
            if (smtpClient.IsConnected)
            {
                await smtpClient.DisconnectAsync(
                    quit: true,
                    CancellationToken.None);
            }
        }
    }
}
