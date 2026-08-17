using NotificationService.Common.Results;

namespace NotificationService.Tests.Common.Results;

public sealed class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResultWithoutMessage()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccessfull);
        Assert.False(result.IsFailure);
        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Null(result.Message);
    }

    [Fact]
    public void GenericSuccess_ExposesFalseValue()
    {
        var result = Result<bool>.Success(false);

        Assert.True(result.IsSuccessfull);
        Assert.False(result.Value);
    }

    [Fact]
    public void GenericFailure_CarriesStatusAndMessage()
    {
        var result = Result<bool>.Failure(
            ResultStatus.NotFound,
            "Inbox message was not found.");

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("Inbox message was not found.", result.Message);
    }

    [Fact]
    public void GenericFailure_CanBeCreatedFromAnotherFailure()
    {
        var source = Result.Failure(ResultStatus.Conflict, "Message already exists.");

        var result = Result<bool>.Failure(source);

        Assert.Equal(source.Status, result.Status);
        Assert.Equal(source.Message, result.Message);
    }

    [Fact]
    public void FailedGenericResult_ThrowsWhenValueIsAccessed()
    {
        var result = Result<bool>.Failure(ResultStatus.Failure, "Processing failed.");

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void FailureFromSuccessfulResult_Throws()
    {
        var source = Result.Success();

        Assert.Throws<ArgumentException>(() => Result<bool>.Failure(source));
    }

    [Fact]
    public void FailureWithSuccessStatus_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Result.Failure(ResultStatus.Success, "Invalid failure."));
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
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
    }
}
