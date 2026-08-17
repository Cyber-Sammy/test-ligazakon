using Microsoft.Extensions.Logging.Abstractions;
using UserService.Application.Common;
using UserService.Application.Common.Exceptions;
using UserService.Application.Common.Results;
using UserService.Application.Interfaces.Infrastructure;
using UserService.Application.Interfaces.IntegrationEvents;
using UserService.Application.Interfaces.UnitOfWork;
using UserService.Application.Models;
using UserService.Domain.Entities;
using ApplicationUserService = UserService.Application.Services.UserService;

namespace UserService.Tests.Application.Services;

public sealed class UserServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 14, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task RegisterUserAsync_ValidCommand_PersistsNormalizedUserAndReturnsId()
    {
        User? persistedUser = null;
        CancellationToken observedToken = default;
        var repository = new StubUserRepository
        {
            OnAdd = user =>
            {
                persistedUser = user;
            }
        };
        var transaction = new StubUnitOfWorkTransaction();
        var unitOfWork = new StubUnitOfWork(transaction)
        {
            SaveChanges = token =>
            {
                observedToken = token;

                if (persistedUser is not null && persistedUser.Id == 0)
                {
                    SetUserId(persistedUser, 42);
                }

                return Task.CompletedTask;
            }
        };
        var outboxWriter = new StubOutboxWriter();
        var service = CreateService(repository, unitOfWork, outboxWriter);
        using var cancellationSource = new CancellationTokenSource();

        var result = await service.RegisterUserAsync(
            CreateCommand(
                firstName: "  Jane ",
                lastName: " Doe  ",
                middleName: " Marie ",
                email: " JANE@Example.COM ",
                phoneNumber: " +380501234567 "),
            cancellationSource.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.NotNull(persistedUser);
        Assert.Equal("Jane", persistedUser.FirstName);
        Assert.Equal("Doe", persistedUser.LastName);
        Assert.Equal("Marie", persistedUser.MiddleName);
        Assert.Equal("jane@example.com", persistedUser.Email);
        Assert.Equal("+380501234567", persistedUser.PhoneNumber);
        Assert.Equal(Now, persistedUser.CreatedAt);
        Assert.Equal(cancellationSource.Token, observedToken);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);
        Assert.Single(outboxWriter.Events);
        Assert.Equal(42, ((UserRegisteredIntegrationEvent)outboxWriter.Events[0]).UserId);
        Assert.Equal(1, transaction.CommitCallCount);
        Assert.Equal(0, transaction.RollbackCallCount);
    }

    [Theory]
    [InlineData(UserUniquenessConflict.Email, "User with email jane@example.com already exists.")]
    [InlineData(UserUniquenessConflict.PhoneNumber, "User with phone number +380501234567 already exists.")]
    [InlineData((UserUniquenessConflict)999, "User already exists.")]
    public async Task RegisterUserAsync_UniquenessConflict_ReturnsConflict(
        UserUniquenessConflict conflict,
        string expectedMessage)
    {
        var repository = new StubUserRepository();
        var transaction = new StubUnitOfWorkTransaction();
        var unitOfWork = new StubUnitOfWork(transaction)
        {
            SaveChanges = _ => throw new UserUniquenessConflictException(
                    conflict,
                    new InvalidOperationException("Database constraint violation."))
        };
        var service = CreateService(repository, unitOfWork);

        var result = await service.RegisterUserAsync(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal(expectedMessage, result.Message);
        Assert.Equal(1, transaction.RollbackCallCount);
        Assert.Equal(0, transaction.CommitCallCount);
    }

    [Fact]
    public async Task RegisterUserAsync_InvalidDomainData_ReturnsValidationErrorWithoutCallingRepository()
    {
        var repository = new StubUserRepository();
        var service = CreateService(repository);

        var result = await service.RegisterUserAsync(
            CreateCommand(phoneNumber: "not-a-phone"),
            CancellationToken.None);

        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Equal(Constants.Results.InvalidRegistrationData, result.Message);
        Assert.Equal(0, repository.AddCallCount);
    }

    [Fact]
    public async Task GetUserByEmailAsync_InvalidEmail_ReturnsValidationErrorWithoutQueryingRepository()
    {
        var repository = new StubUserRepository();
        var service = CreateService(repository);

        var result = await service.GetUserByEmailAsync("not-an-email", CancellationToken.None);

        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Equal(Constants.Results.InvalidEmail, result.Message);
    }

    [Fact]
    public async Task RegisterUserAsync_Cancellation_Propagates()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var repository = new StubUserRepository();
        var unitOfWork = new StubUnitOfWork(new StubUnitOfWorkTransaction())
        {
            SaveChanges = token => Task.FromCanceled(token)
        };
        var service = CreateService(repository, unitOfWork);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.RegisterUserAsync(CreateCommand(), cancellationSource.Token));
    }

    [Fact]
    public async Task GetUserByEmailAsync_NormalizesEmailBeforeQueryingRepository()
    {
        var expectedUser = CreateUser();
        string? observedEmail = null;
        var repository = new StubUserRepository
        {
            GetByEmail = (email, _) =>
            {
                observedEmail = email;
                return Task.FromResult<User?>(expectedUser);
            }
        };
        var service = CreateService(repository);

        var result = await service.GetUserByEmailAsync(
            "  JANE@Example.COM ",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(expectedUser, result.Value);
        Assert.Equal("jane@example.com", observedEmail);
    }

    [Fact]
    public async Task GetUserByEmailAsync_MissingUser_ReturnsNormalizedNotFoundMessage()
    {
        var repository = new StubUserRepository
        {
            GetByEmail = (_, _) => Task.FromResult<User?>(null)
        };
        var service = CreateService(repository);

        var result = await service.GetUserByEmailAsync(
            "  MISSING@Example.COM ",
            CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("User with email missing@example.com was not found.", result.Message);
    }

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsIt()
    {
        var expectedUser = CreateUser();
        int? observedId = null;
        var repository = new StubUserRepository
        {
            GetById = (id, _) =>
            {
                observedId = id;
                return Task.FromResult<User?>(expectedUser);
            }
        };
        var service = CreateService(repository);

        var result = await service.GetUserByIdAsync(42, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(expectedUser, result.Value);
        Assert.Equal(42, observedId);
    }

    [Fact]
    public async Task GetUserByIdAsync_MissingUser_ReturnsNotFound()
    {
        var repository = new StubUserRepository
        {
            GetById = (_, _) => Task.FromResult<User?>(null)
        };
        var service = CreateService(repository);

        var result = await service.GetUserByIdAsync(42, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("User with ID 42 was not found.", result.Message);
    }

    [Fact]
    public async Task GetUserByIdAsync_InvalidId_ReturnsValidationErrorWithoutQueryingRepository()
    {
        var repository = new StubUserRepository();
        var service = CreateService(repository);

        var result = await service.GetUserByIdAsync(0, CancellationToken.None);

        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Equal(Constants.Results.InvalidUserId, result.Message);
    }

    [Fact]
    public async Task GetUsersAsync_EmptyPage_ReturnsSuccessfulEmptyCollection()
    {
        (int Take, int Skip)? observedPagination = null;
        var repository = new StubUserRepository
        {
            Get = (take, skip, _) =>
            {
                observedPagination = (take, skip);
                return Task.FromResult(new List<User>());
            }
        };
        var service = CreateService(repository);

        var result = await service.GetUsersAsync(20, 40, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
        Assert.Equal((20, 40), observedPagination);
    }

    [Fact]
    public async Task GetUsersAsync_RepositoryFails_PropagatesException()
    {
        var expectedException = new InvalidOperationException("Database unavailable.");
        var repository = new StubUserRepository
        {
            Get = (_, _, _) => Task.FromException<List<User>>(expectedException)
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetUsersAsync(20, 0, CancellationToken.None));

        Assert.Same(expectedException, exception);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(101, 0)]
    [InlineData(20, -1)]
    public async Task GetUsersAsync_InvalidPagination_ReturnsValidationErrorWithoutQueryingRepository(
        int take,
        int skip)
    {
        var repository = new StubUserRepository();
        var service = CreateService(repository);

        var result = await service.GetUsersAsync(take, skip, CancellationToken.None);

        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Equal("Take must be between 1 and 100, and skip cannot be negative.", result.Message);
    }

    private static ApplicationUserService CreateService(
        IUserRepository repository,
        IUnitOfWork? unitOfWork = null,
        IOutboxWriter? outboxWriter = null) =>
        new(
            repository,
            NullLogger<ApplicationUserService>.Instance,
            unitOfWork ?? new StubUnitOfWork(new StubUnitOfWorkTransaction()),
            outboxWriter ?? new StubOutboxWriter(),
            new FixedTimeProvider(Now));

    private static RegisterUserCommand CreateCommand(
        string firstName = "Jane",
        string lastName = "Doe",
        string? middleName = null,
        string email = "jane@example.com",
        string phoneNumber = "+380501234567") =>
        new(firstName, lastName, middleName, email, phoneNumber);

    private static User CreateUser() =>
        User.Create("Jane", "Doe", null, "jane@example.com", "+380501234567", Now);

    private static void SetUserId(User user, int id) =>
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, id);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StubUserRepository : IUserRepository
    {
        public Action<User>? OnAdd { get; init; }
        public Func<string, CancellationToken, Task<User?>>? GetByEmail { get; init; }
        public Func<int, CancellationToken, Task<User?>>? GetById { get; init; }
        public Func<int, int, CancellationToken, Task<List<User>>>? Get { get; init; }

        public int AddCallCount { get; private set; }

        public void Add(User user)
        {
            AddCallCount++;
            OnAdd?.Invoke(user);
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            GetByEmail?.Invoke(email, cancellationToken)
            ?? throw new InvalidOperationException("Unexpected repository call.");

        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
            GetById?.Invoke(id, cancellationToken)
            ?? throw new InvalidOperationException("Unexpected repository call.");

        public Task<List<User>> GetAsync(int take, int skip, CancellationToken cancellationToken) =>
            Get?.Invoke(take, skip, cancellationToken)
            ?? throw new InvalidOperationException("Unexpected repository call.");
    }

    private sealed class StubOutboxWriter : IOutboxWriter
    {
        public List<IIntegrationEvent> Events { get; } = [];

        public void Add(IIntegrationEvent integrationEvent) => Events.Add(integrationEvent);
    }

    private sealed class StubUnitOfWork(IUnitOfWorkTransaction transaction) : IUnitOfWork
    {
        public Func<CancellationToken, Task>? SaveChanges { get; init; }

        public int SaveChangesCallCount { get; private set; }

        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
            Task.FromResult(transaction);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return SaveChanges?.Invoke(cancellationToken) ?? Task.CompletedTask;
        }
    }

    private sealed class StubUnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        public int CommitCallCount { get; private set; }
        public int RollbackCallCount { get; private set; }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            CommitCallCount++;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken)
        {
            RollbackCallCount++;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
