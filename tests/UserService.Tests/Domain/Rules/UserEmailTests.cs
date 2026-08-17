using UserService.Domain.Rules;

namespace UserService.Tests.Domain.Rules;

public sealed class UserEmailTests
{
    [Fact]
    public void Normalize_TrimsAndUsesInvariantLowerCase()
    {
        var result = UserEmail.Normalize("  Test.User@EXAMPLE.COM  ");

        Assert.Equal("test.user@example.com", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_NullOrWhitespace_ThrowsArgumentException(string? email)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            UserEmail.Normalize(email!));

        Assert.Equal(nameof(email), exception.ParamName);
    }
}
