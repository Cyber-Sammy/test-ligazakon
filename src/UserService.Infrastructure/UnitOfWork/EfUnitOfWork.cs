using Microsoft.EntityFrameworkCore;
using Npgsql;
using UserService.Application.Common.Exceptions;
using UserService.Application.Interfaces.UnitOfWork;
using UserService.Infrastructure.Common;
using UserService.Infrastructure.Contexts;

namespace UserService.Infrastructure.UnitOfWork;

public class EfUnitOfWork : IUnitOfWork
{
    public EfUnitOfWork(UsersDbContext context)
    {
        _context = context;
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        return new EfUnitOfWorkTransaction(transaction);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            if (exception.InnerException is not PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation
                } postgresException)
            {
                throw;
            }

            var conflict = postgresException.ConstraintName switch
            {
                InfrastructureConstants.Constraints.UsersEmailUnique => UserUniquenessConflict.Email,
                InfrastructureConstants.Constraints.UsersPhoneNumberUnique => UserUniquenessConflict.PhoneNumber,
                _ => (UserUniquenessConflict?)null
            };

            if (conflict is null)
            {
                throw;
            }

            throw new UserUniquenessConflictException(
                conflict.Value,
                exception);
        }
    }

    private readonly UsersDbContext _context;
}
