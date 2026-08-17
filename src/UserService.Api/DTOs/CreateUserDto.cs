using System.ComponentModel.DataAnnotations;
using UserService.Domain.Rules;

namespace UserService.Api.DTOs;

public sealed class CreateUserDto
{
    [Required]
    [StringLength(UserRules.NameMaxLength)]
    public required string FirstName { get; init; }

    [Required]
    [StringLength(UserRules.NameMaxLength)]
    public required string LastName { get; init; }

    [StringLength(UserRules.NameMaxLength)]
    public string? MiddleName { get; init; }

    [Required]
    [EmailAddress]
    [StringLength(UserRules.EmailMaxLength)]
    public required string Email { get; init; }

    [Required]
    [StringLength(UserRules.PhoneNumberMaxLength)]
    [RegularExpression(UserRules.PhoneNumberPattern, 
        ErrorMessage = UserRules.PhoneNumberMustUseE164Format)]
    public required string PhoneNumber { get; init; }
}
