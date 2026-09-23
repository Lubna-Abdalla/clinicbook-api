namespace ClinicBook.Core.Entities;

/// <summary>
/// A patient profile, linked one-to-one to an Identity user by <see cref="UserId"/>.
/// </summary>
public class Patient
{
    public int Id { get; set; }

    /// <summary>Id of the matching Identity user (AspNetUsers.Id). One user = one patient.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>DateOnly because a birth date has no time part and no time zone.</summary>
    public DateOnly DateOfBirth { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
