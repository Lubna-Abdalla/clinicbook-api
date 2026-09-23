using ClinicBook.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBook.Infrastructure.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments", table =>
        {
            table.HasCheckConstraint("CK_Appointments_EndAfterStart", "[EndsAt] > [StartsAt]");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.StartsAt)
            .IsRequired();

        builder.Property(a => a.EndsAt)
            .IsRequired();

        // Store the enum as readable text ("Scheduled") instead of a number,
        // so the data makes sense when reading the table directly in SQL Server.
        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.Reason)
            .HasMaxLength(500);

        builder.Property(a => a.DoctorNotes)
            .HasMaxLength(1000);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        // Null until the appointment is actually cancelled.
        builder.Property(a => a.CancelledAt);

        builder.HasOne(a => a.Doctor)
            .WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId)
            // Restrict keeps the appointment history: a doctor with appointments cannot be deleted,
            // and it also avoids SQL Server's "multiple cascade paths" error.
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // The double-booking guard, enforced by the database itself:
        // a doctor can only have ONE non-cancelled appointment starting at a given moment.
        // The filter is what lets a cancelled appointment free its slot for a new booking.
        builder.HasIndex(a => new { a.DoctorId, a.StartsAt })
            .IsUnique()
            .HasFilter("[Status] <> 'Cancelled'")
            .HasDatabaseName("UX_Appointments_DoctorId_StartsAt_Active");

        // Speeds up "my upcoming appointments" style queries.
        // (A doctor's day view is already served by the unique index above, which starts
        // with the same DoctorId column, so no extra index is needed for it.)
        builder.HasIndex(a => new { a.PatientId, a.StartsAt })
            .HasDatabaseName("IX_Appointments_PatientId_StartsAt");
    }
}
