using Quartz;
using UserService.Infrastructure.Outbox.Abstractions;

namespace UserService.Infrastructure.Outbox.Jobs;

[DisallowConcurrentExecution]
public sealed class OutboxPublisherJob : IJob
{
    public OutboxPublisherJob(IOutboxProcessor outboxProcessor)
    {
        _outboxProcessor = outboxProcessor;
    }

    public Task Execute(IJobExecutionContext context) =>
        _outboxProcessor.ProcessBatchAsync(context.CancellationToken);

    private readonly IOutboxProcessor _outboxProcessor;
}
