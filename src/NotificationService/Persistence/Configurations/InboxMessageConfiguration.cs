using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Common;
using NotificationService.Inbox;

namespace NotificationService.Persistence.Configurations;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable(
            Constants.Inbox.Table,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    Constants.Inbox.VersionCheck,
                    Constants.Inbox.VersionCheckSql);
                tableBuilder.HasCheckConstraint(
                    Constants.Inbox.ProcessingTimeCheck,
                    Constants.Inbox.ProcessingTimeCheckSql);
            });

        builder.HasKey(message => message.EventId);

        builder.Property(message => message.EventId)
            .ValueGeneratedNever();

        builder.Property(message => message.EventType)
            .IsRequired()
            .HasMaxLength(Constants.Inbox.EventTypeMaxLength);

        builder.Property(message => message.EventVersion)
            .IsRequired();

        builder.Property(message => message.ReceivedAtUtc)
            .IsRequired();

        builder.Property(message => message.ProcessedAtUtc)
            .IsRequired();
    }
}
