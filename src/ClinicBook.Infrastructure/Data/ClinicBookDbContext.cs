using ClinicBook.Core.Entities;
using ClinicBook.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClinicBook.Infrastructure.Data;

/// <summary>
/// The EF Core database context: the bridge between the C# entities and the SQL Server tables.
/// It inherits from IdentityDbContext so the Identity tables (AspNetUsers, AspNetRoles, ...)
/// live in the same database as the clinic tables.
/// </summary>
public class ClinicBookDbContext : IdentityDbContext<ApplicationUser>
{
    public ClinicBookDbContext(DbContextOptions<ClinicBookDbContext> options)
        : base(options)
    {
    }

    public DbSet<Specialty> Specialties => Set<Specialty>();

    public DbSet<Doctor> Doctors => Set<Doctor>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Let Identity map its own tables first.
        base.OnModelCreating(modelBuilder);

        // Then pick up every IEntityTypeConfiguration in this project
        // (see the Data/Configurations folder) instead of writing everything in this one method.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicBookDbContext).Assembly);
    }
}
