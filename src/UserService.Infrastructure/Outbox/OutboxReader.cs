using Microsoft.EntityFrameworkCore;
using UserService.Infrastructure.Contexts;
using UserService.Infrastructure.Outbox.Abstractions;

namespace UserService.Infrastructure.Outbox;

public sealed class OutboxReader : IOutboxReader
{
    public OutboxReader(
        UsersDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(DateTimeOffset nowUtc, int batchSize, CancellationToken cancellationToken)
    {
        return await _context.OutboxMessages
            .Where(message =>
                message.PublishedAtUtc == null &&
                (message.NextAttemptAtUtc == null ||
                 message.NextAttemptAtUtc <= nowUtc))
            .OrderBy(message => message.OccurredAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    private readonly UsersDbContext _context;
}
