using System.Text.RegularExpressions;

namespace UserService.Domain.Rules;

public static partial class UserRules
{
    public const int NameMaxLength = 30;
    public const int EmailMaxLength = 50;
    public const int PhoneNumberMaxLength = 16;
    public const string PhoneNumberPattern = @"^\+[1-9]\d{1,14}$";
    public const string PhoneNumberMustUseE164Format =
        "Phone number must be in E.164 format.";

    public static bool IsValidPhoneNumber(string phoneNumber)
    {
        return E164PhoneNumberRegex().IsMatch(phoneNumber);
    }

    [GeneratedRegex(PhoneNumberPattern, RegexOptions.CultureInvariant)]
    private static partial Regex E164PhoneNumberRegex();
}
    
