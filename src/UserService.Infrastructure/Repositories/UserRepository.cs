using Microsoft.EntityFrameworkCore;
using UserService.Application.Interfaces.Infrastructure;
using UserService.Application.Interfaces.UnitOfWork;
using UserService.Domain.Entities;
using UserService.Infrastructure.Contexts;

namespace UserService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    public UserRepository(
        UsersDbContext usersDbContext)
    {
        _usersDbContext = usersDbContext;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var user = await _usersDbContext.Users
            .AsNoTracking()
            .Where(x => x.Email == email)
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var user = await _usersDbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

        return user;
    }

    public async Task<List<User>> GetAsync(int take, int skip, CancellationToken cancellationToken)
    {
        var users = await _usersDbContext.Users
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return users;
    }

    public void Add(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        _usersDbContext.Users.Add(user);
    }

    private readonly UsersDbContext _usersDbContext;
}
