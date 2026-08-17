using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Domain.Rules;
using UserService.Infrastructure.Contexts;

namespace UserService.Tests.Infrastructure;

public sealed class UserConfigurationTests
{
    [Fact]
    public void Model_ContainsRequiredColumnsLengthsAndUniqueIndexes()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(User));

        Assert.NotNull(entity);
        Assert.Equal("Users", entity.GetTableName());
        Assert.False(entity.FindProperty(nameof(User.FirstName))!.IsNullable);
        Assert.Equal(
            UserRules.NameMaxLength,
            entity.FindProperty(nameof(User.FirstName))!.GetMaxLength());
        Assert.False(entity.FindProperty(nameof(User.LastName))!.IsNullable);
        Assert.Equal(
            UserRules.NameMaxLength,
            entity.FindProperty(nameof(User.MiddleName))!.GetMaxLength());
        Assert.False(entity.FindProperty(nameof(User.Email))!.IsNullable);
        Assert.Equal(
            UserRules.EmailMaxLength,
            entity.FindProperty(nameof(User.Email))!.GetMaxLength());
        Assert.False(entity.FindProperty(nameof(User.PhoneNumber))!.IsNullable);
        Assert.Equal(
            UserRules.PhoneNumberMaxLength,
            entity.FindProperty(nameof(User.PhoneNumber))!.GetMaxLength());

        var indexes = entity.GetIndexes().ToDictionary(index => index.GetDatabaseName()!);
        Assert.True(indexes["UX_Users_Email"].IsUnique);
        Assert.Equal(
            nameof(User.Email),
            Assert.Single(indexes["UX_Users_Email"].Properties).Name);
        Assert.True(indexes["UX_Users_PhoneNumber"].IsUnique);
        Assert.Equal(
            nameof(User.PhoneNumber),
            Assert.Single(indexes["UX_Users_PhoneNumber"].Properties).Name);
    }

    private static UsersDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new UsersDbContext(options);
    }
}
