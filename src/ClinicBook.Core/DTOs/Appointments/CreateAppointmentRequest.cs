using System.ComponentModel.DataAnnotations;

namespace ClinicBook.Core.DTOs.Appointments;

/// <summary>
/// A booking request. The client only picks the doctor and the start time;
/// the end time is calculated by the server from the doctor's slot length,
/// so a client can never invent an overlapping or backwards time range.
/// </summary>
public class CreateAppointmentRequest
{
    [Range(1, int.MaxValue)]
    public int DoctorId { get; set; }

    [Required]
    public DateTime StartsAt { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}
