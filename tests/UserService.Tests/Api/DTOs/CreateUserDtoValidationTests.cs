using System.ComponentModel.DataAnnotations;
using UserService.Api.DTOs;
using UserService.Domain.Rules;

namespace UserService.Tests.Api.DTOs;

public sealed class CreateUserDtoValidationTests
{
    [Fact]
    public void ValidDto_PassesValidation()
    {
        var validationResults = Validate(CreateDto());

        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData("firstName")]
    [InlineData("lastName")]
    [InlineData("email")]
    [InlineData("phoneNumber")]
    public void RequiredFieldIsEmpty_FailsValidation(string field)
    {
        var dto = CreateDto(
            firstName: field == "firstName" ? string.Empty : "Jane",
            lastName: field == "lastName" ? string.Empty : "Doe",
            email: field == "email" ? string.Empty : "jane@example.com",
            phoneNumber: field == "phoneNumber" ? string.Empty : "+380501234567");

        Assert.NotEmpty(Validate(dto));
    }

    [Fact]
    public void InvalidEmail_FailsValidation()
    {
        var validationResults = Validate(CreateDto(email: "not-an-email"));

        Assert.Contains(validationResults, result =>
            result.MemberNames.Contains(nameof(CreateUserDto.Email)));
    }

    [Theory]
    [InlineData("380501234567")]
    [InlineData("+080501234567")]
    [InlineData("+1")]
    [InlineData("+1234567890123456")]
    public void InvalidPhoneNumber_FailsValidation(string phoneNumber)
    {
        var validationResults = Validate(CreateDto(phoneNumber: phoneNumber));

        Assert.Contains(validationResults, result =>
            result.MemberNames.Contains(nameof(CreateUserDto.PhoneNumber)));
    }

    [Theory]
    [InlineData("firstName")]
    [InlineData("lastName")]
    [InlineData("middleName")]
    [InlineData("email")]
    [InlineData("phoneNumber")]
    public void FieldExceedsMaximumLength_FailsValidation(string field)
    {
        var dto = CreateDto(
            firstName: field == "firstName" ? new string('a', UserRules.NameMaxLength + 1) : "Jane",
            lastName: field == "lastName" ? new string('a', UserRules.NameMaxLength + 1) : "Doe",
            middleName: field == "middleName" ? new string('a', UserRules.NameMaxLength + 1) : null,
            email: field == "email" ? $"{new string('a', UserRules.EmailMaxLength)}@example.com" : "jane@example.com",
            phoneNumber: field == "phoneNumber" ? $"+{new string('1', UserRules.PhoneNumberMaxLength)}" : "+380501234567");

        Assert.NotEmpty(Validate(dto));
    }

    private static List<ValidationResult> Validate(CreateUserDto dto)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            validationResults,
            validateAllProperties: true);

        return validationResults;
    }

    private static CreateUserDto CreateDto(
        string firstName = "Jane",
        string lastName = "Doe",
        string? middleName = null,
        string email = "jane@example.com",
        string phoneNumber = "+380501234567") =>
        new()
        {
            FirstName = firstName,
            LastName = lastName,
            MiddleName = middleName,
            Email = email,
            PhoneNumber = phoneNumber
        };
}
