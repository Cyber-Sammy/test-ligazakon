namespace NotificationService;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Notification Service worker started");
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
