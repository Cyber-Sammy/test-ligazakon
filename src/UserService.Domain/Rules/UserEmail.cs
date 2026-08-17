namespace UserService.Domain.Rules;

public static class UserEmail
{
    public static string Normalize(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return email.Trim().ToLowerInvariant();
    }
}