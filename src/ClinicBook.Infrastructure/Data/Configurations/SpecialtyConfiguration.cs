using ClinicBook.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBook.Infrastructure.Data.Configurations;

public class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.ToTable("Specialties");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Two specialties must never share a name.
        builder.HasIndex(s => s.Name)
            .IsUnique();

        // A short starter list, so a fresh database is immediately usable.
        builder.HasData(
            new Specialty { Id = 1, Name = "General Practice" },
            new Specialty { Id = 2, Name = "Cardiology" },
            new Specialty { Id = 3, Name = "Dermatology" },
            new Specialty { Id = 4, Name = "Pediatrics" },
            new Specialty { Id = 5, Name = "Orthopedics" });
    }
}
