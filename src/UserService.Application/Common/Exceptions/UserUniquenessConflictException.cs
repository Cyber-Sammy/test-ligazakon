namespace UserService.Application.Common.Exceptions;

public enum UserUniquenessConflict
{
    Email,
    PhoneNumber
}

public sealed class UserUniquenessConflictException : Exception
{
    public UserUniquenessConflictException(UserUniquenessConflict conflict, Exception innerException)
        : base(
            string.Format(
                global::UserService.Application.Common.Constants.Exceptions.UserUniquenessConflict,
                conflict),
            innerException)
    {
        Conflict = conflict;
    }

    public UserUniquenessConflict Conflict { get; }
}
