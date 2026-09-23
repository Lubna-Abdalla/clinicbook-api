using ClinicBook.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBook.Infrastructure.Data.Configurations;

public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.ToTable("DoctorSchedules", table =>
        {
            // The same rules the DoctorSchedule entity checks in C#, enforced by the database too.
            table.HasCheckConstraint("CK_DoctorSchedules_EndAfterStart", "[EndTime] > [StartTime]");
            table.HasCheckConstraint(
                "CK_DoctorSchedules_SlotMinutes",
                $"[SlotMinutes] BETWEEN {DoctorSchedule.MinSlotMinutes} AND {DoctorSchedule.MaxSlotMinutes}");
        });

        builder.HasKey(s => s.Id);

        // System.DayOfWeek is stored as its int value (Sunday = 0).
        builder.Property(s => s.DayOfWeek)
            .IsRequired();

        // TimeOnly maps to the SQL Server "time" type.
        builder.Property(s => s.StartTime)
            .IsRequired();

        builder.Property(s => s.EndTime)
            .IsRequired();

        builder.Property(s => s.SlotMinutes)
            .IsRequired();

        // A doctor has exactly one working window per weekday.
        builder.HasIndex(s => new { s.DoctorId, s.DayOfWeek })
            .IsUnique();

        builder.HasOne(s => s.Doctor)
            .WithMany(d => d.Schedules)
            .HasForeignKey(s => s.DoctorId)
            // A schedule is not history: if the doctor row goes, its schedules go with it.
            .OnDelete(DeleteBehavior.Cascade);
    }
}
