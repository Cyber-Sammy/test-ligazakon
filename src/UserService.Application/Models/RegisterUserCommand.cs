namespace UserService.Application.Models;

public sealed record RegisterUserCommand(
    string FirstName,
    string LastName,
    string? MiddleName,
    string Email,
    string PhoneNumber);
