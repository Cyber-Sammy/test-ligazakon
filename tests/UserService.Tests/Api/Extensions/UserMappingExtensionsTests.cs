using UserService.Api.Extensions;
using UserService.Domain.Entities;

namespace UserService.Tests.Api.Extensions;

public sealed class UserMappingExtensionsTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 14, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public void ToGetUserDto_UserWithMiddleName_ProducesFullName()
    {
        var user = CreateUser("Marie");

        var dto = user.ToGetUserDto();

        Assert.Equal("Doe Jane Marie", dto.FullName);
        Assert.Equal(user.Email, dto.Email);
        Assert.Equal(user.PhoneNumber, dto.PhoneNumber);
    }

    [Fact]
    public void ToGetUserDto_UserWithoutMiddleName_DoesNotAddExtraWhitespace()
    {
        var dto = CreateUser(null).ToGetUserDto();

        Assert.Equal("Doe Jane", dto.FullName);
    }

    [Fact]
    public void ToGetUserDtos_EmptySequence_ReturnsEmptyReadOnlyList()
    {
        var result = Array.Empty<User>().ToGetUserDtos();

        Assert.Empty(result);
        Assert.IsAssignableFrom<IReadOnlyList<UserService.Api.DTOs.GetUserDto>>(result);
    }

    private static User CreateUser(string? middleName) =>
        User.Create(
            "Jane",
            "Doe",
            middleName,
            "jane@example.com",
            "+380501234567",
            CreatedAt);
}
