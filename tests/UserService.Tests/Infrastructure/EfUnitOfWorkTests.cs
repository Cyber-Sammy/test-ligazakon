using Microsoft.EntityFrameworkCore;
using Npgsql;
using UserService.Application.Common.Exceptions;
using UserService.Domain.Entities;
using UserService.Infrastructure.Contexts;
using UserService.Infrastructure.UnitOfWork;

namespace UserService.Tests.Infrastructure;

public sealed class EfUnitOfWorkTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 14, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveChangesAsync_TrackedUser_PersistsItAndPopulatesGeneratedId()
    {
        await using var context = CreateContext();
        var unitOfWork = new EfUnitOfWork(context);
        var user = CreateUser();
        context.Users.Add(user);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.True(user.Id > 0);
        Assert.Equal(1, await context.Users.CountAsync());
    }

    [Theory]
    [InlineData(nameof(User.Email), UserUniquenessConflict.Email)]
    [InlineData(nameof(User.PhoneNumber), UserUniquenessConflict.PhoneNumber)]
    public async Task SaveChangesAsync_KnownUniqueViolation_TranslatesToApplicationException(
        string propertyName,
        UserUniquenessConflict expectedConflict)
    {
        var constraintName = GetUniqueConstraintName(propertyName);
        var databaseException = CreateDbUpdateException(
            PostgresErrorCodes.UniqueViolation,
            constraintName);
        await using var context = CreateThrowingContext(databaseException);
        var unitOfWork = new EfUnitOfWork(context);

        var exception = await Assert.ThrowsAsync<UserUniquenessConflictException>(() =>
            unitOfWork.SaveChangesAsync(CancellationToken.None));

        Assert.Equal(expectedConflict, exception.Conflict);
        Assert.Same(databaseException, exception.InnerException);
    }

    [Fact]
    public async Task SaveChangesAsync_UnknownUniqueConstraint_RethrowsOriginalException()
    {
        var databaseException = CreateDbUpdateException(
            PostgresErrorCodes.UniqueViolation,
            "UX_Unexpected");
        await using var context = CreateThrowingContext(databaseException);
        var unitOfWork = new EfUnitOfWork(context);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            unitOfWork.SaveChangesAsync(CancellationToken.None));

        Assert.Same(databaseException, exception);
    }

    [Fact]
    public async Task SaveChangesAsync_NonUniqueDatabaseFailure_RethrowsOriginalException()
    {
        var databaseException = CreateDbUpdateException(
            PostgresErrorCodes.NotNullViolation,
            constraintName: null);
        await using var context = CreateThrowingContext(databaseException);
        var unitOfWork = new EfUnitOfWork(context);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            unitOfWork.SaveChangesAsync(CancellationToken.None));

        Assert.Same(databaseException, exception);
    }

    private static string GetUniqueConstraintName(string propertyName)
    {
        using var context = CreateContext();
        var userEntity = context.Model.FindEntityType(typeof(User))!;
        var index = userEntity.GetIndexes().Single(candidate =>
            candidate.IsUnique &&
            candidate.Properties.Count == 1 &&
            candidate.Properties[0].Name == propertyName);

        return index.GetDatabaseName()!;
    }

    private static UsersDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new UsersDbContext(options);
    }

    private static ThrowingUsersDbContext CreateThrowingContext(Exception exception)
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ThrowingUsersDbContext(options, exception);
    }

    private static User CreateUser() =>
        User.Create(
            "Jane",
            "Doe",
            null,
            "jane@example.com",
            "+380501234567",
            CreatedAt);

    private static DbUpdateException CreateDbUpdateException(
        string sqlState,
        string? constraintName)
    {
        var postgresException = new PostgresException(
            "Database error.",
            "ERROR",
            "ERROR",
            sqlState,
            null,
            null,
            0,
            0,
            null,
            null,
            "public",
            "Users",
            null,
            null,
            constraintName,
            null,
            null,
            null);

        return new DbUpdateException("Save failed.", postgresException);
    }

    private sealed class ThrowingUsersDbContext(
        DbContextOptions<UsersDbContext> options,
        Exception exception)
        : UsersDbContext(options)
    {
        public override Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromException<int>(exception);
    }
}
