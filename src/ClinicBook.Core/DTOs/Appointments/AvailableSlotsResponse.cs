namespace ClinicBook.Core.DTOs.Appointments;

/// <summary>
/// The free slot start times for one doctor on one calendar date.
/// A patient can send any of these values straight back as CreateAppointmentRequest.StartsAt.
/// </summary>
public record AvailableSlotsResponse(
    int DoctorId,
    DateOnly Date,
    int SlotMinutes,
    IReadOnlyList<DateTime> AvailableSlots);
