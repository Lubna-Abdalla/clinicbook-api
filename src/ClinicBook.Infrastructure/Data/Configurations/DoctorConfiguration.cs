using ClinicBook.Core.Entities;
using ClinicBook.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBook.Infrastructure.Data.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.UserId)
            .IsRequired()
            .HasMaxLength(450);

        // One Identity user can own at most one doctor profile.
        builder.HasIndex(d => d.UserId)
            .IsUnique();

        builder.Property(d => d.Bio)
            .HasMaxLength(1000);

        // Money needs an exact type: 10 digits in total, 2 after the decimal point.
        builder.Property(d => d.ConsultationFee)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(d => d.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(d => d.Specialty)
            .WithMany(s => s.Doctors)
            .HasForeignKey(d => d.SpecialtyId)
            // Restrict: a specialty that still has doctors cannot be deleted.
            .OnDelete(DeleteBehavior.Restrict);

        // The link to the login account. There is no navigation property on Doctor,
        // which keeps the Core project free of any Identity/EF Core reference.
        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<Doctor>(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
