namespace NotificationService.Options;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; init; } = string.Empty;

    public int Port { get; init; }

    public string SenderName { get; init; } = string.Empty;

    public string SenderAddress { get; init; } = string.Empty;

    public bool UseSsl { get; init; }
}
