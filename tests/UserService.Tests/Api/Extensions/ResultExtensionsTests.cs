using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserService.Api.Extensions;
using UserService.Application.Common;
using UserService.Application.Common.Results;

namespace UserService.Tests.Api.Extensions;

public sealed class ResultExtensionsTests
{
    private readonly TestController _controller = new();

    [Fact]
    public void GenericSuccess_WithoutCallback_ReturnsOkWithValue()
    {
        var actionResult = Result<int>.Success(42).ToActionResult(_controller);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(42, okResult.Value);
    }

    [Fact]
    public void GenericSuccess_WithCallback_UsesCallbackResult()
    {
        var actionResult = Result<int>.Success(42).ToActionResult(
            _controller,
            value => _controller.Created("/items/42", value));

        var createdResult = Assert.IsType<CreatedResult>(actionResult);
        Assert.Equal(42, createdResult.Value);
    }

    [Fact]
    public void NonGenericSuccess_WithoutCallback_ReturnsNoContent()
    {
        var actionResult = Result.Success().ToActionResult(_controller);

        Assert.IsType<NoContentResult>(actionResult);
    }

    [Theory]
    [InlineData(ResultStatus.ValidationError, StatusCodes.Status400BadRequest, "Validation error")]
    [InlineData(ResultStatus.Unauthorized, StatusCodes.Status401Unauthorized, "Unauthorized")]
    [InlineData(ResultStatus.Forbidden, StatusCodes.Status403Forbidden, "Forbidden")]
    [InlineData(ResultStatus.NotFound, StatusCodes.Status404NotFound, "Resource not found")]
    [InlineData(ResultStatus.Conflict, StatusCodes.Status409Conflict, "Conflict")]
    [InlineData(ResultStatus.Failure, StatusCodes.Status500InternalServerError, "An unexpected error occurred")]
    public void Failure_MapsStatusAndProblemDetails(
        ResultStatus status,
        int expectedStatusCode,
        string expectedTitle)
    {
        var result = Result<int>.Failure(status, "Failure detail.");

        var actionResult = result.ToActionResult(_controller);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(expectedStatusCode, problemDetails.Status);
        Assert.Equal(expectedTitle, problemDetails.Title);
        Assert.Equal("Failure detail.", problemDetails.Detail);
    }

    private sealed class TestController : ControllerBase;
}
