namespace ClinicBook.Core.Entities;

/// <summary>
/// A doctor profile. The login details (name, email, password) live in the Identity user
/// table; this entity only stores the clinic-specific data and links to that user by <see cref="UserId"/>.
/// </summary>
public class Doctor
{
    public int Id { get; set; }

    /// <summary>Id of the matching Identity user (AspNetUsers.Id). One user = one doctor.</summary>
    public string UserId { get; set; } = string.Empty;

    public int SpecialtyId { get; set; }

    public string? Bio { get; set; }

    public decimal ConsultationFee { get; set; }

    /// <summary>Inactive doctors stay in the database (history is kept) but cannot take new bookings.</summary>
    public bool IsActive { get; set; } = true;

    public Specialty? Specialty { get; set; }

    public ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
