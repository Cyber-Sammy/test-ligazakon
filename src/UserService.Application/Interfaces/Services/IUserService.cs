using UserService.Application.Common.Results;
using UserService.Application.Models;
using UserService.Domain.Entities;

namespace UserService.Application.Interfaces.Services;

public interface IUserService
{
    Task<Result<int>> RegisterUserAsync(RegisterUserCommand command, CancellationToken cancellationToken);

    Task<Result<User>> GetUserByEmailAsync(string email, CancellationToken cancellationToken);

    Task<Result<User>> GetUserByIdAsync(int id, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<User>>> GetUsersAsync(int take, int skip, CancellationToken cancellationToken);
}
