using Microsoft.EntityFrameworkCore.Storage;
using UserService.Application.Interfaces.UnitOfWork;

namespace UserService.Infrastructure.UnitOfWork;

public class EfUnitOfWorkTransaction : IUnitOfWorkTransaction
{
    public EfUnitOfWorkTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await _transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        await _transaction.RollbackAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _transaction.DisposeAsync();
    }

    private readonly IDbContextTransaction _transaction;
}
