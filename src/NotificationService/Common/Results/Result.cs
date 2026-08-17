namespace NotificationService.Common.Results;

public enum ResultStatus
{
    Success,
    ValidationError,
    NotFound,
    Conflict,
    Failure
}

public class Result
{
    private protected Result(ResultStatus status, string? message)
    {
        if (status == ResultStatus.Success && message is not null)
        {
            throw new ArgumentException(
                Constants.Results.SuccessCannotContainMessage,
                nameof(message));
        }

        if (status != ResultStatus.Success && string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                Constants.Results.FailureMustContainMessage,
                nameof(message));
        }

        Status = status;
        Message = message;
    }

    public ResultStatus Status { get; }

    public string? Message { get; }

    public bool IsSuccessfull => Status == ResultStatus.Success;

    public bool IsFailure => !IsSuccessfull;

    public static Result Success() => new(ResultStatus.Success, null);

    public static Result Failure(ResultStatus status, string message)
    {
        EnsureFailureStatus(status);
        return new Result(status, message);
    }

    public static Result Failure(Result source)
    {
        EnsureFailure(source);
        return new Result(source.Status, source.Message);
    }

    private protected static void EnsureFailure(Result source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.IsSuccessfull)
        {
            throw new ArgumentException(
                Constants.Results.CannotCreateFailureFromSuccess,
                nameof(source));
        }
    }

    private protected static void EnsureFailureStatus(ResultStatus status)
    {
        if (status == ResultStatus.Success)
        {
            throw new ArgumentException(
                Constants.Results.SuccessStatusCannotRepresentFailure,
                nameof(status));
        }
    }
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue value)
        : base(ResultStatus.Success, null)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
    }

    private Result(ResultStatus status, string message)
        : base(status, message)
    {
    }

    public TValue Value => IsSuccessfull
        ? _value!
        : throw new InvalidOperationException(Constants.Results.FailedResultHasNoValue);

    public static Result<TValue> Success(TValue value) => new(value);

    public new static Result<TValue> Failure(ResultStatus status, string message)
    {
        EnsureFailureStatus(status);
        return new Result<TValue>(status, message);
    }

    public new static Result<TValue> Failure(Result source)
    {
        EnsureFailure(source);
        return new Result<TValue>(source.Status, source.Message!);
    }
}
