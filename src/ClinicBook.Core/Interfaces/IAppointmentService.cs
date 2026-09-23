using ClinicBook.Core.DTOs.Appointments;

namespace ClinicBook.Core.Interfaces;

public interface IAppointmentService
{
    /// <summary>Slot start times a doctor still has free on the given date.</summary>
    Task<AvailableSlotsResponse> GetAvailableSlotsAsync(int doctorId, DateOnly date);

    /// <summary>Books a slot for the patient belonging to <paramref name="patientUserId"/>.</summary>
    Task<AppointmentDto> BookAsync(string patientUserId, CreateAppointmentRequest request);

    /// <summary>The caller's own appointments, whether they are a patient or a doctor.</summary>
    Task<IReadOnlyList<AppointmentDto>> GetForUserAsync(string userId);

    Task<AppointmentDto> GetByIdAsync(int id, string userId, bool isAdmin);

    Task<AppointmentDto> CancelAsync(int id, string userId, bool isAdmin);

    Task<AppointmentDto> CompleteAsync(int id, string doctorUserId, CompleteAppointmentRequest request);

    Task<AppointmentDto> MarkAsNoShowAsync(int id, string doctorUserId);
}
