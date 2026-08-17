using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using UserService.Api.DTOs;
using UserService.Application.Common;
using UserService.Application.Common.Results;
using UserService.Application.Interfaces.Services;
using UserService.Application.Models;
using UserService.Domain.Entities;

namespace UserService.Tests.Api;

public sealed class UsersApiTests
{
    [Fact]
    public async Task RegisterUser_ValidRequest_ReturnsCreatedWithLocationAndId()
    {
        RegisterUserCommand? observedCommand = null;
        var service = new StubUserService
        {
            Register = (command, _) =>
            {
                observedCommand = command;
                return Task.FromResult(Result<int>.Success(42));
            }
        };
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", CreateUserRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/users/42", response.Headers.Location?.AbsolutePath);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(42, body.RootElement.GetProperty("id").GetInt32());
        Assert.NotNull(observedCommand);
        Assert.Equal("Jane", observedCommand.FirstName);
        Assert.Equal("jane@example.com", observedCommand.Email);
    }

    [Fact]
    public async Task RegisterUser_InvalidRequest_ReturnsValidationProblemWithoutCallingService()
    {
        var service = new StubUserService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        var invalidRequest = CreateUserRequest(
            email: "not-an-email",
            phoneNumber: "0501234567");

        var response = await client.PostAsJsonAsync("/api/users", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(nameof(CreateUserDto.Email), problem.Errors.Keys);
        Assert.Contains(nameof(CreateUserDto.PhoneNumber), problem.Errors.Keys);
        Assert.Equal(0, service.RegisterCallCount);
    }

    [Theory]
    [InlineData("firstName")]
    [InlineData("lastName")]
    [InlineData("email")]
    [InlineData("phoneNumber")]
    public async Task RegisterUser_WhitespaceRequiredField_ReturnsBadRequest(string field)
    {
        var service = new StubUserService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        var request = CreateUserRequest(
            firstName: field == "firstName" ? "   " : "Jane",
            lastName: field == "lastName" ? "   " : "Doe",
            email: field == "email" ? "   " : "jane@example.com",
            phoneNumber: field == "phoneNumber" ? "   " : "+380501234567");

        var response = await client.PostAsJsonAsync("/api/users", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, service.RegisterCallCount);
    }

    [Fact]
    public async Task RegisterUser_Conflict_ReturnsConflictProblemDetails()
    {
        var service = new StubUserService
        {
            Register = (_, _) => Task.FromResult(Result<int>.Failure(
                ResultStatus.Conflict,
                "User with email jane@example.com already exists."))
        };
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", CreateUserRequest());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Conflict", problem?.Title);
        Assert.Equal(
            "User with email jane@example.com already exists.",
            problem?.Detail);
    }

    [Fact]
    public async Task GetUserById_ExistingUser_ReturnsMappedUser()
    {
        var service = new StubUserService
        {
            GetById = (id, _) => Task.FromResult(
                id == 42
                    ? Result<User>.Success(CreateUser("Marie"))
                    : Result<User>.Failure(ResultStatus.NotFound, "Missing."))
        };
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users/42");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<GetUserDto>();
        Assert.Equal("Doe Jane Marie", user?.FullName);
        Assert.Equal("jane@example.com", user?.Email);
    }

    [Fact]
    public async Task GetUserById_MissingUser_ReturnsNotFoundProblemDetails()
    {
        var service = new StubUserService
        {
            GetById = (_, _) => Task.FromResult(Result<User>.Failure(
                ResultStatus.NotFound,
                "User with ID 42 was not found."))
        };
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users/42");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Resource not found", problem?.Title);
    }

    [Fact]
    public async Task GetUserById_NonPositiveId_DoesNotMatchRoute()
    {
        var service = new StubUserService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users/0");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, service.GetByIdCallCount);
    }

    [Fact]
    public async Task GetUserByEmail_ValidEmail_UsesRouteValueAndReturnsUser()
    {
        var service = new StubUserService
        {
            GetByEmail = (email, _) =>
            {
                Assert.Equal("jane@example.com", email);
                return Task.FromResult(Result<User>.Success(CreateUser()));
            }
        };
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/users/by-email/jane%40example.com");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<GetUserDto>();
        Assert.Equal("jane@example.com", user?.Email);
        Assert.Equal(1, service.GetByEmailCallCount);
    }

    [Fact]
    public async Task GetUserByEmail_InvalidEmail_ReturnsBadRequestWithoutCallingService()
    {
        var service = new StubUserService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users/by-email/not-an-email");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, service.GetByEmailCallCount);
    }

    [Fact]
    public async Task GetUsers_EmptyPage_ReturnsEmptyArray()
    {
        var service = new StubUserService
        {
            Get = (take, skip, _) =>
            {
                Assert.Equal(20, take);
                Assert.Equal(40, skip);
                return Task.FromResult(Result<IReadOnlyList<User>>.Success([]));
            }
        };
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users?take=20&skip=40");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<GetUserDto>>();
        Assert.NotNull(users);
        Assert.Empty(users);
    }

    [Theory]
    [InlineData("/api/users?skip=0")]
    [InlineData("/api/users?take=0&skip=0")]
    [InlineData("/api/users?take=101&skip=0")]
    [InlineData("/api/users?take=20&skip=-1")]
    public async Task GetUsers_InvalidPagination_ReturnsBadRequest(string requestUri)
    {
        var service = new StubUserService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, service.GetCallCount);
    }

    [Fact]
    public async Task Request_WithValidCorrelationId_EchoesItInResponse()
    {
        const string correlationId = "integration-test-correlation-id";
        var service = new StubUserService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(Constants.Correlation.HeaderName, correlationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            correlationId,
            Assert.Single(response.Headers.GetValues(Constants.Correlation.HeaderName)));
    }

    [Fact]
    public async Task Request_WithoutCorrelationId_ReturnsGeneratedCorrelationId()
    {
        var service = new StubUserService();
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var correlationId = Assert.Single(
            response.Headers.GetValues(Constants.Correlation.HeaderName));
        Assert.True(Guid.TryParseExact(
            correlationId,
            Constants.Correlation.IdFormat,
            out _));
    }

    [Fact]
    public async Task UnexpectedServiceException_ReturnsSanitizedServerError()
    {
        var service = new StubUserService
        {
            GetById = (_, _) => throw new InvalidOperationException(
                "Sensitive database connection details.")
        };
        await using var factory = CreateFactory(service);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users/42");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(Constants.ProblemDetails.ServerErrorTitle, problem?.Title);
        Assert.Equal(Constants.ProblemDetails.UnexpectedErrorDetail, problem?.Detail);
        Assert.DoesNotContain("Sensitive", await response.Content.ReadAsStringAsync());
    }

    private static WebApplicationFactory<Program> CreateFactory(IUserService userService) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OutboxProcessing:Enabled"] = "false"
                }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IUserService>();
                services.AddSingleton(userService);
            });
        });

    private static CreateUserDto CreateUserRequest(
        string firstName = "Jane",
        string lastName = "Doe",
        string email = "jane@example.com",
        string phoneNumber = "+380501234567") =>
        new()
        {
            FirstName = firstName,
            LastName = lastName,
            MiddleName = (string?)null,
            Email = email,
            PhoneNumber = phoneNumber
        };

    private static User CreateUser(string? middleName = null) =>
        User.Create(
            "Jane",
            "Doe",
            middleName,
            "jane@example.com",
            "+380501234567",
            new DateTimeOffset(2026, 8, 14, 12, 30, 0, TimeSpan.Zero));

    private sealed class StubUserService : IUserService
    {
        public Func<RegisterUserCommand, CancellationToken, Task<Result<int>>>? Register { get; init; }
        public Func<string, CancellationToken, Task<Result<User>>>? GetByEmail { get; init; }
        public Func<int, CancellationToken, Task<Result<User>>>? GetById { get; init; }
        public Func<int, int, CancellationToken, Task<Result<IReadOnlyList<User>>>>? Get { get; init; }

        public int RegisterCallCount { get; private set; }
        public int GetByEmailCallCount { get; private set; }
        public int GetByIdCallCount { get; private set; }
        public int GetCallCount { get; private set; }

        public Task<Result<int>> RegisterUserAsync(
            RegisterUserCommand command,
            CancellationToken cancellationToken)
        {
            RegisterCallCount++;
            return Register?.Invoke(command, cancellationToken)
                ?? throw new InvalidOperationException("Unexpected service call.");
        }

        public Task<Result<User>> GetUserByEmailAsync(
            string email,
            CancellationToken cancellationToken)
        {
            GetByEmailCallCount++;
            return GetByEmail?.Invoke(email, cancellationToken)
                ?? throw new InvalidOperationException("Unexpected service call.");
        }

        public Task<Result<User>> GetUserByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            GetByIdCallCount++;
            return GetById?.Invoke(id, cancellationToken)
                ?? throw new InvalidOperationException("Unexpected service call.");
        }

        public Task<Result<IReadOnlyList<User>>> GetUsersAsync(
            int take,
            int skip,
            CancellationToken cancellationToken)
        {
            GetCallCount++;
            return Get?.Invoke(take, skip, cancellationToken)
                ?? throw new InvalidOperationException("Unexpected service call.");
        }
    }
}
