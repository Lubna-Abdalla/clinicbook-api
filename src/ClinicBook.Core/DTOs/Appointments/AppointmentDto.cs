namespace ClinicBook.Core.DTOs.Appointments;

public record AppointmentDto(
    int Id,
    int DoctorId,
    string DoctorName,
    string SpecialtyName,
    int PatientId,
    string PatientName,
    DateTime StartsAt,
    DateTime EndsAt,
    string Status,
    string? Reason,
    string? DoctorNotes,
    DateTime CreatedAt,
    DateTime? CancelledAt);
