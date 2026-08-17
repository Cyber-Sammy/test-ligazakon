using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Infrastructure.Contexts;
using UserService.Infrastructure.Repositories;

namespace UserService.Tests.Infrastructure;

public sealed class UserRepositoryTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 14, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task Add_ValidUser_StagesItWithoutPersisting()
    {
        await using var context = CreateContext();
        var repository = new UserRepository(context);
        var user = CreateUser("jane@example.com", "+380501234567");

        repository.Add(user);

        Assert.Equal(EntityState.Added, context.Entry(user).State);

        context.ChangeTracker.Clear();
        Assert.Empty(await context.Users.ToListAsync());
    }

    [Fact]
    public async Task GetByEmailAsync_ExistingUser_ReturnsItWithoutTracking()
    {
        await using var context = CreateContext();
        var user = CreateUser("jane@example.com", "+380501234567");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new UserRepository(context);

        var result = await repository.GetByEmailAsync(
            "jane@example.com",
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("jane@example.com", result.Email);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByEmailAsync_DifferentCase_DoesNotMatchNormalizedStoredEmail()
    {
        await using var context = CreateContext();
        context.Users.Add(CreateUser("jane@example.com", "+380501234567"));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new UserRepository(context);

        var result = await repository.GetByEmailAsync(
            "JANE@EXAMPLE.COM",
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_MissingUser_ReturnsNull()
    {
        await using var context = CreateContext();
        var repository = new UserRepository(context);

        var result = await repository.GetByIdAsync(999, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_AppliesStableIdOrderingAndOffsetPagination()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            CreateUser("one@example.com", "+380501234561"),
            CreateUser("two@example.com", "+380501234562"),
            CreateUser("three@example.com", "+380501234563"),
            CreateUser("four@example.com", "+380501234564"));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new UserRepository(context);

        var result = await repository.GetAsync(2, 1, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal([2, 3], result.Select(user => user.Id));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    private static UsersDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new UsersDbContext(options);
    }

    private static User CreateUser(string email, string phoneNumber) =>
        User.Create("Jane", "Doe", null, email, phoneNumber, CreatedAt);
}
