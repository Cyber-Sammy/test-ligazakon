using UserService.Domain.Rules;

namespace UserService.Domain.Entities;

public sealed class User
{
    private User() { }

    private User(
        string firstName,
        string lastName,
        string? middleName,
        string email,
        string phoneNumber,
        DateTimeOffset createdAt)
    {
        FirstName = firstName;
        LastName = lastName;
        MiddleName = middleName;
        Email = email;
        PhoneNumber = phoneNumber;
        CreatedAt = createdAt;
    }

    public int Id { get; private set; }

    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public string? MiddleName { get; private set; }

    public string Email { get; private set; } = null!;

    public string PhoneNumber { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public static User Create(
        string firstName,
        string lastName,
        string? middleName,
        string email,
        string phoneNumber,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        var normalizedFirstName = firstName.Trim();
        var normalizedLastName = lastName.Trim();
        var normalizedEmail = UserEmail.Normalize(email);
        var normalizedPhoneNumber = phoneNumber.Trim();
        var normalizedMiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim();

        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            normalizedFirstName.Length,
            UserRules.NameMaxLength);

        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            normalizedLastName.Length,
            UserRules.NameMaxLength);

        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            normalizedEmail.Length,
            UserRules.EmailMaxLength);

        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            normalizedPhoneNumber.Length,
            UserRules.PhoneNumberMaxLength);

        if (normalizedMiddleName is not null)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(
                normalizedMiddleName.Length,
                UserRules.NameMaxLength);
        }

        if (!UserRules.IsValidPhoneNumber(normalizedPhoneNumber))
        {
            throw new ArgumentException(
                UserRules.PhoneNumberMustUseE164Format,
                nameof(phoneNumber));
        }

        return new User(
            normalizedFirstName,
            normalizedLastName,
            normalizedMiddleName,
            normalizedEmail,
            normalizedPhoneNumber,
            createdAt);
    }
}
