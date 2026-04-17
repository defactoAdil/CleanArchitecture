using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasIndex(e => e.RegistrationNumber)
            .IsUnique();

        builder.Property(e => e.RegistrationNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.IdentityNumber)
            .HasMaxLength(11)
            .IsRequired();

        builder.Property(e => e.Firstname)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Lastname)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PersonalMobileNumber)
            .HasMaxLength(12);

        builder.Property(e => e.SourceTypeStr)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ActivePassiveCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(e => e.CompanyName)
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);
    }
}
