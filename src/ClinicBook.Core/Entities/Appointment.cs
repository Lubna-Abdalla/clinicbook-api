using ClinicBook.Core.Enums;
using ClinicBook.Core.Exceptions;

namespace ClinicBook.Core.Entities;

/// <summary>
/// A booking of one patient with one doctor at one point in time.
/// Appointments are never deleted: cancelling only changes the status, so history is preserved.
/// </summary>
public class Appointment
{
    public int Id { get; set; }

    public int DoctorId { get; set; }

    public int PatientId { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    public string? Reason { get; set; }

    public string? DoctorNotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public Doctor? Doctor { get; set; }

    public Patient? Patient { get; set; }

    /// <summary>
    /// A cancelled appointment frees its time slot again; every other status still occupies it.
    /// </summary>
    public bool OccupiesTimeSlot => Status != AppointmentStatus.Cancelled;

    /// <summary>
    /// Creates a new scheduled appointment, rejecting a time range that does not make sense.
    /// A factory method keeps this rule in one place instead of repeating it in the API layer.
    /// </summary>
    public static Appointment Schedule(
        int doctorId,
        int patientId,
        DateTime startsAt,
        DateTime endsAt,
        string? reason,
        DateTime createdAt)
    {
        if (endsAt <= startsAt)
        {
            throw new BusinessRuleException("Appointment end time must be after the start time.");
        }

        return new Appointment
        {
            DoctorId = doctorId,
            PatientId = patientId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Reason = reason,
            Status = AppointmentStatus.Scheduled,
            CreatedAt = createdAt
        };
    }

    /// <summary>Cancels a still-scheduled appointment and records when it happened.</summary>
    public void Cancel(DateTime cancelledAt)
    {
        if (Status == AppointmentStatus.Cancelled)
        {
            throw new BusinessRuleException("This appointment is already cancelled.");
        }

        if (Status != AppointmentStatus.Scheduled)
        {
            throw new BusinessRuleException(
                $"A {Status} appointment can no longer be cancelled.");
        }

        Status = AppointmentStatus.Cancelled;
        CancelledAt = cancelledAt;
    }

    /// <summary>Marks the visit as done, optionally storing the doctor's notes.</summary>
    public void Complete(string? doctorNotes = null)
    {
        if (Status != AppointmentStatus.Scheduled)
        {
            throw new BusinessRuleException(
                $"Only a scheduled appointment can be completed, but this one is {Status}.");
        }

        Status = AppointmentStatus.Completed;
        DoctorNotes = doctorNotes;
    }

    /// <summary>Marks that the patient did not turn up.</summary>
    public void MarkAsNoShow()
    {
        if (Status != AppointmentStatus.Scheduled)
        {
            throw new BusinessRuleException(
                $"Only a scheduled appointment can be marked as a no-show, but this one is {Status}.");
        }

        Status = AppointmentStatus.NoShow;
    }
}
