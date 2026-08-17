using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Domain.Entities;
using UserService.Domain.Rules;
using UserService.Infrastructure.Common;

namespace UserService.Infrastructure.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable(InfrastructureConstants.UsersTable);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(UserRules.NameMaxLength);

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(UserRules.NameMaxLength);

        builder.Property(x => x.MiddleName)
            .HasMaxLength(UserRules.NameMaxLength);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(UserRules.EmailMaxLength);

        builder.Property(x => x.PhoneNumber)
            .IsRequired()
            .HasMaxLength(UserRules.PhoneNumberMaxLength);

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName(InfrastructureConstants.Constraints.UsersEmailUnique);

        builder.HasIndex(user => user.PhoneNumber)
            .IsUnique()
            .HasDatabaseName(InfrastructureConstants.Constraints.UsersPhoneNumberUnique);
    }
}
