namespace UserService.Application.Common;

public static class Constants
{
    public static class Results
    {
        public const string SuccessCannotContainMessage = "A successful result cannot contain an error message.";
        public const string FailureMustContainMessage = "A failed result must contain an error message.";
        public const string FailedResultHasNoValue = "A failed result does not contain a value.";
        public const string CannotCreateFailureFromSuccess = "A failed result cannot be created from a successful result.";
        public const string SuccessStatusCannotRepresentFailure = "Success status cannot be used to create a failed result.";
        public const string UserRegistrationFailure = "User registration failed.";
        public const string UserAlreadyExists = "User already exists.";
        public const string EmailAlreadyExists = "User with email {0} already exists.";
        public const string PhoneNumberAlreadyExists = "User with phone number {0} already exists.";
        public const string UserNotFound = "User with email {0} was not found.";
        public const string UserNotFoundById = "User with ID {0} was not found.";
        public const string InvalidRegistrationData = "Registration data is invalid.";
        public const string InvalidEmail = "Email address is invalid.";
        public const string InvalidUserId = "User ID must be greater than zero.";
        public const string InvalidPagination =
            "Take must be between {0} and {1}, and skip cannot be negative.";
    }

    public static class Pagination
    {
        public const int MinimumTake = 1;
        public const int MaximumTake = 100;
        public const int MinimumSkip = 0;
    }

    public static class Correlation
    {
        public const string HeaderName = "X-Correlation-ID";
        public const string LogPropertyName = "CorrelationId";
        public const string IdFormat = "N";
    }

    public static class Logging
    {
        public const string UnhandledException = "Unhandled exception occurred.";
        public const string UserRegistrationStarted = "User registration started.";
        public const string UserRegistrationCompleted = "User registration completed with ID {UserId}.";
        public const string UserAlreadyExists = "User with email {Email} or phone number {PhoneNumber} already exists.";
        public const string GettingUserByEmail = "Getting user by email {Email}.";
        public const string UserNotFound = "User with email {Email} was not found.";
        public const string UserRetrieved = "Successfully retrieved user with email {Email}.";
        public const string GettingUserById = "Getting user by ID {UserId}.";
        public const string UserNotFoundById = "User with ID {UserId} was not found.";
        public const string UserRetrievedById = "Successfully retrieved user with ID {UserId}.";
        public const string GettingUsers = "Getting {Take} users, skipping {Skip} users.";
        public const string UsersRetrieved = "{UserCount} users retrieved.";
    }

    public static class ProblemDetails
    {
        public const string ServerErrorTitle = "Server Error";
        public const string ValidationErrorTitle = "Validation error";
        public const string UnauthorizedTitle = "Unauthorized";
        public const string ForbiddenTitle = "Forbidden";
        public const string NotFoundTitle = "Resource not found";
        public const string ConflictTitle = "Conflict";
        public const string UnexpectedErrorTitle = "An unexpected error occurred";
        public const string UnexpectedErrorDetail = "An unexpected error occurred while processing the request.";
    }

    public static class Exceptions
    {
        public const string UserUniquenessConflict =
            "User uniqueness conflict: {0}.";
    }

    public static class IntegrationEvents
    {
        public const string UserRegisteredType = "user.registered";
        public const int UserRegisteredVersion = 1;
    }

    public static class Routes
    {
        public const string Health = "/health";
        public const string ScalarPrefix = "/scalar";
        public const string OpenApiPrefix = "/openapi";
    }

    public static class Health
    {
        public const string HealthyStatus = "healthy";
    }

}
