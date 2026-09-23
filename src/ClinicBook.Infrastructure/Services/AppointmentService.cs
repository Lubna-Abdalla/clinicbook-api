using ClinicBook.Core.DTOs.Appointments;
using ClinicBook.Core.Entities;
using ClinicBook.Core.Enums;
using ClinicBook.Core.Exceptions;
using ClinicBook.Core.Interfaces;
using ClinicBook.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ClinicBook.Infrastructure.Services;

/// <summary>
/// All booking logic lives here: which slots are free, who may book, and who may change an appointment.
/// </summary>
public class AppointmentService : IAppointmentService
{
    // SQL Server error numbers for "unique index/constraint violated".
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    private readonly ClinicBookDbContext _db;

    public AppointmentService(ClinicBookDbContext db)
    {
        _db = db;
    }

    public async Task<AvailableSlotsResponse> GetAvailableSlotsAsync(int doctorId, DateOnly date)
    {
        var doctor = await _db.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == doctorId)
            ?? throw NotFoundException.For("Doctor", doctorId);

        // The weekly schedule repeats, so the weekday of the requested date decides the working window.
        var schedule = await _db.DoctorSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.DayOfWeek == date.DayOfWeek);

        if (!doctor.IsActive || schedule is null)
        {
            return new AvailableSlotsResponse(doctorId, date, 0, []);
        }

        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var nextDayStart = dayStart.AddDays(1);

        var appointmentsThatDay = await _db.Appointments
            .AsNoTracking()
            .Where(a => a.DoctorId == doctorId && a.StartsAt >= dayStart && a.StartsAt < nextDayStart)
            .ToListAsync();

        // Which of those appointments still block a slot is a domain rule,
        // so the DoctorSchedule entity decides it (and the unit tests cover it).
        var availableSlots = schedule.GetAvailableSlotStartTimes(date, appointmentsThatDay, DateTime.UtcNow);

        return new AvailableSlotsResponse(doctorId, date, schedule.SlotMinutes, availableSlots);
    }

    public async Task<AppointmentDto> BookAsync(string patientUserId, CreateAppointmentRequest request)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == patientUserId)
            ?? throw new NotFoundException("No patient profile was found for the signed-in user.");

        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == request.DoctorId)
            ?? throw NotFoundException.For("Doctor", request.DoctorId);

        if (!doctor.IsActive)
        {
            throw new BusinessRuleException("This doctor is not currently accepting appointments.");
        }

        var startsAt = request.StartsAt;

        if (startsAt <= DateTime.UtcNow)
        {
            throw new BusinessRuleException("Appointments can only be booked for a future time.");
        }

        var schedule = await _db.DoctorSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.DoctorId == doctor.Id && s.DayOfWeek == startsAt.DayOfWeek)
            ?? throw new BusinessRuleException($"This doctor does not work on {startsAt.DayOfWeek}.");

        if (!schedule.HasSlotStartingAt(TimeOnly.FromDateTime(startsAt)))
        {
            throw new BusinessRuleException(
                $"{startsAt:HH:mm} is not a valid slot start time. " +
                $"The doctor works {schedule.StartTime:HH\\:mm}-{schedule.EndTime:HH\\:mm} " +
                $"in {schedule.SlotMinutes} minute slots.");
        }

        // The end time is derived from the doctor's slot length, never sent by the client.
        var endsAt = startsAt.AddMinutes(schedule.SlotMinutes);

        // Rule: a doctor cannot have two active appointments starting at the same time.
        var slotTaken = await _db.Appointments.AnyAsync(a =>
            a.DoctorId == doctor.Id
            && a.StartsAt == startsAt
            && a.Status != AppointmentStatus.Cancelled);

        if (slotTaken)
        {
            throw new BusinessRuleException("That time slot is already booked. Please choose another one.");
        }

        // Rule: the patient cannot be in two places at once.
        var patientBusy = await _db.Appointments.AnyAsync(a =>
            a.PatientId == patient.Id
            && a.Status != AppointmentStatus.Cancelled
            && a.StartsAt < endsAt
            && startsAt < a.EndsAt);

        if (patientBusy)
        {
            throw new BusinessRuleException("You already have an appointment that overlaps this time.");
        }

        var appointment = Appointment.Schedule(
            doctor.Id, patient.Id, startsAt, endsAt, request.Reason?.Trim(), DateTime.UtcNow);

        _db.Appointments.Add(appointment);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Safety net for two people booking the same slot at the same moment:
            // the filtered unique index in the database rejects the loser of the race.
            throw new BusinessRuleException("That time slot was just booked by someone else. Please choose another one.");
        }

        return await GetDtoAsync(appointment.Id);
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetForUserAsync(string userId)
    {
        // The same endpoint serves both sides: a patient sees their bookings,
        // a doctor sees the appointments booked with them.
        var appointments = _db.Appointments
            .AsNoTracking()
            .Where(a => a.Patient!.UserId == userId || a.Doctor!.UserId == userId);

        var rows = await ProjectAppointments(appointments).ToListAsync();

        return rows.Select(MapToDto).ToList();
    }

    public async Task<AppointmentDto> GetByIdAsync(int id, string userId, bool isAdmin)
    {
        var row = await FindRowAsync(id);
        EnsureParticipant(row, userId, isAdmin);

        return MapToDto(row);
    }

    public async Task<AppointmentDto> CancelAsync(int id, string userId, bool isAdmin)
    {
        var row = await FindRowAsync(id, asNoTracking: false);
        EnsureParticipant(row, userId, isAdmin);

        // The entity itself decides whether cancelling is allowed, and records when it happened.
        // Nothing is deleted, so the history stays in the database.
        row.Appointment.Cancel(DateTime.UtcNow);
        await _db.SaveChangesAsync();

        return MapToDto(row);
    }

    public async Task<AppointmentDto> CompleteAsync(int id, string doctorUserId, CompleteAppointmentRequest request)
    {
        var row = await FindRowAsync(id, asNoTracking: false);
        EnsureOwningDoctor(row, doctorUserId);

        row.Appointment.Complete(request.DoctorNotes?.Trim());
        await _db.SaveChangesAsync();

        return MapToDto(row);
    }

    public async Task<AppointmentDto> MarkAsNoShowAsync(int id, string doctorUserId)
    {
        var row = await FindRowAsync(id, asNoTracking: false);
        EnsureOwningDoctor(row, doctorUserId);

        row.Appointment.MarkAsNoShow();
        await _db.SaveChangesAsync();

        return MapToDto(row);
    }

    private async Task<AppointmentRow> FindRowAsync(int id, bool asNoTracking = true)
    {
        // When the caller is about to change the appointment we need EF Core to track it,
        // so that SaveChanges writes the new status back.
        var appointments = asNoTracking ? _db.Appointments.AsNoTracking() : _db.Appointments;

        return await ProjectAppointments(appointments.Where(a => a.Id == id)).FirstOrDefaultAsync()
            ?? throw NotFoundException.For("Appointment", id);
    }

    private async Task<AppointmentDto> GetDtoAsync(int id) => MapToDto(await FindRowAsync(id));

    /// <summary>
    /// Only the patient, the doctor of the appointment, or an admin may see or cancel it.
    /// </summary>
    private static void EnsureParticipant(AppointmentRow row, string userId, bool isAdmin)
    {
        if (!isAdmin && row.PatientUserId != userId && row.DoctorUserId != userId)
        {
            throw new ForbiddenException("This appointment does not belong to you.");
        }
    }

    private static void EnsureOwningDoctor(AppointmentRow row, string doctorUserId)
    {
        if (row.DoctorUserId != doctorUserId)
        {
            throw new ForbiddenException("Only the doctor of this appointment can do that.");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException sql
        && sql.Number is UniqueIndexViolation or UniqueConstraintViolation;

    /// <summary>
    /// Names live in the Identity user table, so the query joins the doctor's and the patient's user rows.
    /// </summary>
    private IQueryable<AppointmentRow> ProjectAppointments(IQueryable<Appointment> appointments)
    {
        return from appointment in appointments
               join doctorUser in _db.Users on appointment.Doctor!.UserId equals doctorUser.Id
               join patientUser in _db.Users on appointment.Patient!.UserId equals patientUser.Id
               orderby appointment.StartsAt descending
               select new AppointmentRow(
                   appointment,
                   doctorUser.Id,
                   doctorUser.FullName,
                   appointment.Doctor!.Specialty!.Name,
                   patientUser.Id,
                   patientUser.FullName);
    }

    private static AppointmentDto MapToDto(AppointmentRow row)
    {
        var appointment = row.Appointment;

        return new AppointmentDto(
            appointment.Id,
            appointment.DoctorId,
            row.DoctorName,
            row.SpecialtyName,
            appointment.PatientId,
            row.PatientName,
            appointment.StartsAt,
            appointment.EndsAt,
            appointment.Status.ToString(),
            appointment.Reason,
            appointment.DoctorNotes,
            appointment.CreatedAt,
            appointment.CancelledAt);
    }

    /// <summary>One appointment plus the few user columns the API needs to show with it.</summary>
    private sealed record AppointmentRow(
        Appointment Appointment,
        string DoctorUserId,
        string DoctorName,
        string SpecialtyName,
        string PatientUserId,
        string PatientName);
}
