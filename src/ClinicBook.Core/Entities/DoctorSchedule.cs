using ClinicBook.Core.Exceptions;

namespace ClinicBook.Core.Entities;

/// <summary>
/// A doctor's recurring weekly working window, e.g. "every Monday 09:00-13:00 in 30 minute slots".
/// It is not a calendar date: the same row applies to every Monday.
/// </summary>
public class DoctorSchedule
{
    public const int MinSlotMinutes = 5;
    public const int MaxSlotMinutes = 120;

    public int Id { get; set; }

    public int DoctorId { get; set; }

    /// <summary>Built-in System.DayOfWeek (Sunday = 0), stored as an int.</summary>
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>TimeOnly because this is a time of day with no date attached.</summary>
    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    /// <summary>Length of one appointment slot, in minutes.</summary>
    public int SlotMinutes { get; set; }

    public Doctor? Doctor { get; set; }

    /// <summary>How long the doctor works on that weekday.</summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Checks the rules a schedule must always satisfy and throws if one is broken.
    /// Called before saving so invalid schedules never reach the database.
    /// </summary>
    public void Validate()
    {
        if (EndTime <= StartTime)
        {
            throw new BusinessRuleException("Schedule end time must be after the start time.");
        }

        if (SlotMinutes < MinSlotMinutes || SlotMinutes > MaxSlotMinutes)
        {
            throw new BusinessRuleException(
                $"Slot length must be between {MinSlotMinutes} and {MaxSlotMinutes} minutes.");
        }

        if (SlotMinutes > Duration.TotalMinutes)
        {
            throw new BusinessRuleException(
                "Slot length cannot be longer than the working window it belongs to.");
        }
    }

    /// <summary>
    /// Splits the working window into slot start times, e.g. 09:00, 09:30, 10:00 ...
    /// A slot is only produced if it fits completely inside the window.
    /// </summary>
    public IReadOnlyList<TimeOnly> GetSlotStartTimes()
    {
        Validate();

        var slots = new List<TimeOnly>();
        var windowStart = (int)StartTime.ToTimeSpan().TotalMinutes;
        var windowEnd = (int)EndTime.ToTimeSpan().TotalMinutes;

        for (var minute = windowStart; minute + SlotMinutes <= windowEnd; minute += SlotMinutes)
        {
            slots.Add(new TimeOnly(minute / 60, minute % 60));
        }

        return slots;
    }

    /// <summary>True when the given time of day is one of this schedule's slot start times.</summary>
    public bool HasSlotStartingAt(TimeOnly time) => GetSlotStartTimes().Contains(time);

    /// <summary>
    /// The slots still free on a given date: every slot of the window, minus the ones already
    /// taken and minus anything in the past.
    /// A cancelled appointment does not occupy its slot, so cancelling makes the slot bookable again.
    /// </summary>
    public IReadOnlyList<DateTime> GetAvailableSlotStartTimes(
        DateOnly date,
        IEnumerable<Appointment> appointmentsThatDay,
        DateTime nowUtc)
    {
        // The schedule repeats weekly, so it only applies to dates falling on its weekday.
        if (date.DayOfWeek != DayOfWeek)
        {
            return [];
        }

        var occupiedSlots = appointmentsThatDay
            .Where(appointment => appointment.OccupiesTimeSlot)
            .Select(appointment => appointment.StartsAt)
            .ToHashSet();

        return GetSlotStartTimes()
            .Select(time => date.ToDateTime(time))
            .Where(slot => slot > nowUtc && !occupiedSlots.Contains(slot))
            .ToList();
    }
}
