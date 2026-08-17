using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Infrastructure.Common;
using UserService.Infrastructure.Outbox;

namespace UserService.Infrastructure.Configuration;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable(
            InfrastructureConstants.Outbox.Table,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    InfrastructureConstants.Outbox.VersionCheck,
                    InfrastructureConstants.Outbox.VersionCheckSql);
                tableBuilder.HasCheckConstraint(
                    InfrastructureConstants.Outbox.AttemptsCheck,
                    InfrastructureConstants.Outbox.AttemptsCheckSql);
            });

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .ValueGeneratedNever();

        builder.Property(message => message.Type)
            .IsRequired()
            .HasMaxLength(InfrastructureConstants.Outbox.TypeMaxLength);

        builder.Property(message => message.Version)
            .IsRequired();

        builder.Property(message => message.Payload)
            .IsRequired()
            .HasColumnType(InfrastructureConstants.Outbox.JsonbColumnType);

        builder.Property(message => message.PartitionKey)
            .IsRequired()
            .HasMaxLength(InfrastructureConstants.Outbox.PartitionKeyMaxLength);

        builder.Property(message => message.OccurredAtUtc)
            .IsRequired();

        builder.Property(message => message.PublishedAtUtc);

        builder.Property(message => message.Attempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(message => message.NextAttemptAtUtc);

        builder.Property(message => message.LastError)
            .HasMaxLength(InfrastructureConstants.Outbox.LastErrorMaxLength);

        builder.HasIndex(message => new
            {
                message.NextAttemptAtUtc,
                message.OccurredAtUtc
            })
            .HasDatabaseName(InfrastructureConstants.Outbox.PendingIndex)
            .HasFilter(InfrastructureConstants.Outbox.PendingIndexFilter);
    }
}
