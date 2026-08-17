namespace UserService.Application.Common.Results;

public enum ResultStatus
{
    Success,
    ValidationError,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Failure
}
