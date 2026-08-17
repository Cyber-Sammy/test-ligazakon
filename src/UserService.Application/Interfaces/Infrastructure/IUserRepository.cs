using UserService.Domain.Entities;

namespace UserService.Application.Interfaces.Infrastructure;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<List<User>> GetAsync(int take, int skip, CancellationToken cancellationToken);

    void Add(User user);
}