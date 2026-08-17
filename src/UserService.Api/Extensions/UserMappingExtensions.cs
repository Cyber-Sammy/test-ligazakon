using UserService.Api.DTOs;
using UserService.Domain.Entities;

namespace UserService.Api.Extensions;

public static class UserMappingExtensions
{
    public static GetUserDto ToGetUserDto(this User user) => new()
    {
        FullName = string.Join(
            ' ',
            new[] { user.LastName, user.FirstName, user.MiddleName }
                .Where(namePart => !string.IsNullOrWhiteSpace(namePart))),
        Email = user.Email,
        PhoneNumber = user.PhoneNumber
    };

    public static IReadOnlyList<GetUserDto> ToGetUserDtos(this IEnumerable<User> users) =>
        users.Select(user => user.ToGetUserDto()).ToList();
}
