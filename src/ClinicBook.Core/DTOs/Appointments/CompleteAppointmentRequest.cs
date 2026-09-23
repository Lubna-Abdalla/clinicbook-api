using System.ComponentModel.DataAnnotations;

namespace ClinicBook.Core.DTOs.Appointments;

public class CompleteAppointmentRequest
{
    [StringLength(1000)]
    public string? DoctorNotes { get; set; }
}
