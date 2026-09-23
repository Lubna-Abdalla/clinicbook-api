using System.ComponentModel.DataAnnotations;
using ClinicBook.Core.Entities;

namespace ClinicBook.Core.DTOs.Schedules;

/// <summary>
/// Adds one recurring weekly working window for a doctor, e.g. every Tuesday 09:00-13:00.
/// </summary>
public class CreateDoctorScheduleRequest
{
    [Range(0, 6, ErrorMessage = "DayOfWeek must be between 0 (Sunday) and 6 (Saturday).")]
    public DayOfWeek DayOfWeek { get; set; }

    // Nullable for the same reason as RegisterRequest.DateOfBirth: without it, a missing
    // start time would quietly become midnight instead of a 400 response.
    [Required]
    public TimeOnly? StartTime { get; set; }

    [Required]
    public TimeOnly? EndTime { get; set; }

    [Range(DoctorSchedule.MinSlotMinutes, DoctorSchedule.MaxSlotMinutes)]
    public int SlotMinutes { get; set; } = 30;
}
