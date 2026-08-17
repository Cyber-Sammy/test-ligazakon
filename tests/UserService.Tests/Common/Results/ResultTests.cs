using UserService.Application.Common.Results;

namespace UserService.Tests.Common.Results;

public sealed class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResultWithoutMessage()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Null(result.Message);
    }

    [Fact]
    public void GenericSuccess_ExposesValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_CarriesStatusAndMessage()
    {
        var result = Result<int>.Failure(
            ResultStatus.NotFound,
            "User was not found.");

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("User was not found.", result.Message);
    }

    [Fact]
    public void GenericFailure_CanBeCreatedFromAnotherFailure()
    {
        var source = Result.Failure(
            ResultStatus.Conflict,
            "User already exists.");

        var result = Result<int>.Failure(source);

        Assert.Equal(source.Status, result.Status);
        Assert.Equal(source.Message, result.Message);
    }

    [Fact]
    public void FailedGenericResult_ThrowsWhenValueIsAccessed()
    {
        var result = Result<int>.Failure(
            ResultStatus.Failure,
            "User operation failed.");

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void FailureFromSuccessfulResult_Throws()
    {
        var source = Result.Success();

        Assert.Throws<ArgumentException>(() => Result<int>.Failure(source));
    }

    [Fact]
    public void FailureWithSuccessStatus_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Result<int>.Failure(ResultStatus.Success, "Invalid failure."));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FailureWithoutMessage_Throws(string? message)
    {
        Assert.Throws<ArgumentException>(() =>
            Result.Failure(ResultStatus.Failure, message!));
    }

    [Fact]
    public void GenericSuccessWithNullValue_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Result<string>.Success(null!));
    }

    [Fact]
    public void NonGenericFailure_CanBeCreatedFromAnotherFailure()
    {
        var source = Result.Failure(ResultStatus.ValidationError, "Invalid input.");

        var result = Result.Failure(source);

        Assert.Equal(source.Status, result.Status);
        Assert.Equal(source.Message, result.Message);
    }

    [Fact]
    public void FailureFromNullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
        Assert.Throws<ArgumentNullException>(() => Result<int>.Failure(null!));
    }
}
