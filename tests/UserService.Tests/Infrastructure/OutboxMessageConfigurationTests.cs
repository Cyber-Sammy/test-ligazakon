using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using UserService.Infrastructure.Contexts;
using UserService.Infrastructure.Outbox;

namespace UserService.Tests.Infrastructure;

public sealed class OutboxMessageConfigurationTests
{
    [Fact]
    public void Model_ConfiguresOutboxPersistenceAndPendingIndex()
    {
        using var context = CreateContext();
        var designTimeModel = context.GetService<IDesignTimeModel>().Model;
        var entity = designTimeModel.FindEntityType(typeof(OutboxMessage));

        Assert.NotNull(entity);
        Assert.Equal("OutboxMessages", entity.GetTableName());
        Assert.Equal(
            Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never,
            entity.FindProperty(nameof(OutboxMessage.Id))!.ValueGenerated);
        Assert.False(entity.FindProperty(nameof(OutboxMessage.Type))!.IsNullable);
        Assert.Equal(200, entity.FindProperty(nameof(OutboxMessage.Type))!.GetMaxLength());
        Assert.False(entity.FindProperty(nameof(OutboxMessage.PartitionKey))!.IsNullable);
        Assert.Equal(200, entity.FindProperty(nameof(OutboxMessage.PartitionKey))!.GetMaxLength());
        Assert.Equal(
            "jsonb",
            entity.FindProperty(nameof(OutboxMessage.Payload))!.GetColumnType());
        Assert.Equal(
            0,
            entity.FindProperty(nameof(OutboxMessage.Attempts))!.GetDefaultValue());
        Assert.Equal(
            2000,
            entity.FindProperty(nameof(OutboxMessage.LastError))!.GetMaxLength());

        var pendingIndex = Assert.Single(entity.GetIndexes());
        Assert.Equal("IX_OutboxMessages_Pending", pendingIndex.GetDatabaseName());
        Assert.Equal(
            [nameof(OutboxMessage.NextAttemptAtUtc), nameof(OutboxMessage.OccurredAtUtc)],
            pendingIndex.Properties.Select(property => property.Name));
        Assert.Equal("\"PublishedAtUtc\" IS NULL", pendingIndex.GetFilter());

        var checkConstraints = entity.GetCheckConstraints()
            .ToDictionary(constraint => constraint.Name!);
        Assert.Equal(
            "\"Version\" > 0",
            checkConstraints["CK_OutboxMessages_Version_Positive"].Sql);
        Assert.Equal(
            "\"Attempts\" >= 0",
            checkConstraints["CK_OutboxMessages_Attempts_NonNegative"].Sql);
    }

    private static UsersDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=metadata-only;Username=test;Password=test")
            .Options;

        return new UsersDbContext(options);
    }
}
