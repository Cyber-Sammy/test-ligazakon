using UserService.Domain.Entities;
using UserService.Domain.Rules;

namespace UserService.Tests.Domain.Entities;

public sealed class UserTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 14, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Create_NormalizesAllSupportedFields()
    {
        var user = CreateUser(
            firstName: "  Jane  ",
            lastName: "  Doe  ",
            middleName: "  Marie  ",
            email: "  Jane.Doe@Example.COM  ",
            phoneNumber: "  +380501234567  ");

        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("Marie", user.MiddleName);
        Assert.Equal("jane.doe@example.com", user.Email);
        Assert.Equal("+380501234567", user.PhoneNumber);
        Assert.Equal(CreatedAt, user.CreatedAt);
        Assert.Equal(0, user.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankMiddleName_NormalizesItToNull(string? middleName)
    {
        var user = CreateUser(middleName: middleName);

        Assert.Null(user.MiddleName);
    }

    [Theory]
    [InlineData("firstName", null)]
    [InlineData("firstName", "")]
    [InlineData("firstName", "   ")]
    [InlineData("lastName", null)]
    [InlineData("lastName", "")]
    [InlineData("lastName", "   ")]
    [InlineData("email", null)]
    [InlineData("email", "")]
    [InlineData("email", "   ")]
    [InlineData("phoneNumber", null)]
    [InlineData("phoneNumber", "")]
    [InlineData("phoneNumber", "   ")]
    public void Create_RequiredFieldIsBlank_ThrowsArgumentException(
        string field,
        string? value)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() => CreateUser(
            firstName: field == "firstName" ? value! : "Jane",
            lastName: field == "lastName" ? value! : "Doe",
            email: field == "email" ? value! : "jane@example.com",
            phoneNumber: field == "phoneNumber" ? value! : "+380501234567"));

        Assert.Equal(field, exception.ParamName);
    }

    [Theory]
    [InlineData("firstName")]
    [InlineData("lastName")]
    [InlineData("middleName")]
    [InlineData("email")]
    [InlineData("phoneNumber")]
    public void Create_FieldExceedsMaximumLength_ThrowsArgumentOutOfRangeException(
        string field)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CreateUser(
            firstName: field == "firstName" ? new string('a', UserRules.NameMaxLength + 1) : "Jane",
            lastName: field == "lastName" ? new string('a', UserRules.NameMaxLength + 1) : "Doe",
            middleName: field == "middleName" ? new string('a', UserRules.NameMaxLength + 1) : null,
            email: field == "email"
                ? $"{new string('a', UserRules.EmailMaxLength - "@test.com".Length + 1)}@test.com"
                : "jane@example.com",
            phoneNumber: field == "phoneNumber" ? $"+{new string('1', UserRules.PhoneNumberMaxLength)}" : "+380501234567"));

        Assert.NotNull(exception);
    }

    [Fact]
    public void Create_FieldsAtMaximumLength_Succeeds()
    {
        var user = CreateUser(
            firstName: new string('a', UserRules.NameMaxLength),
            lastName: new string('b', UserRules.NameMaxLength),
            middleName: new string('c', UserRules.NameMaxLength),
            email: $"{new string('a', UserRules.EmailMaxLength - "@test.com".Length)}@test.com",
            phoneNumber: $"+{new string('1', UserRules.PhoneNumberMaxLength - 1)}");

        Assert.Equal(UserRules.NameMaxLength, user.FirstName.Length);
        Assert.Equal(UserRules.EmailMaxLength, user.Email.Length);
        Assert.Equal(UserRules.PhoneNumberMaxLength, user.PhoneNumber.Length);
    }

    [Theory]
    [InlineData("380501234567")]
    [InlineData("+080501234567")]
    [InlineData("+1")]
    [InlineData("+380 50 123")]
    [InlineData("+380-50-123")]
    [InlineData("+abc")]
    public void Create_InvalidE164PhoneNumber_ThrowsArgumentException(string phoneNumber)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            CreateUser(phoneNumber: phoneNumber));

        Assert.Equal(nameof(phoneNumber), exception.ParamName);
        Assert.Contains(UserRules.PhoneNumberMustUseE164Format, exception.Message);
    }

    private static User CreateUser(
        string firstName = "Jane",
        string lastName = "Doe",
        string? middleName = null,
        string email = "jane@example.com",
        string phoneNumber = "+380501234567") =>
        User.Create(firstName, lastName, middleName, email, phoneNumber, CreatedAt);
}
