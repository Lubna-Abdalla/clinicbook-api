namespace ClinicBook.Core.DTOs.Schedules;

public record DoctorScheduleDto(
    int Id,
    int DoctorId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotMinutes);
