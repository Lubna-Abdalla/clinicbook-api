namespace ClinicBook.Core.Enums;

/// <summary>
/// The lifecycle states an appointment can be in.
/// Stored in the database as text (e.g. "Scheduled") so the data stays readable in SQL.
/// </summary>
public enum AppointmentStatus
{
    Scheduled = 0,
    Completed = 1,
    Cancelled = 2,
    NoShow = 3
}
