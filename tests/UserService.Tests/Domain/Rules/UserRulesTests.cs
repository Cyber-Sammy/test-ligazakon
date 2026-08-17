using UserService.Domain.Rules;

namespace UserService.Tests.Domain.Rules;

public sealed class UserRulesTests
{
    [Theory]
    [InlineData("+12")]
    [InlineData("+380501234567")]
    [InlineData("+123456789012345")]
    public void IsValidPhoneNumber_ValidE164Number_ReturnsTrue(string phoneNumber)
    {
        Assert.True(UserRules.IsValidPhoneNumber(phoneNumber));
    }

    [Theory]
    [InlineData("")]
    [InlineData("+1")]
    [InlineData("380501234567")]
    [InlineData("+0123456789")]
    [InlineData("+1234567890123456")]
    [InlineData("+380 50 1234567")]
    [InlineData("+380-50-1234567")]
    [InlineData(" +380501234567")]
    public void IsValidPhoneNumber_InvalidE164Number_ReturnsFalse(string phoneNumber)
    {
        Assert.False(UserRules.IsValidPhoneNumber(phoneNumber));
    }
}
