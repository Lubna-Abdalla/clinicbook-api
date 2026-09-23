using ClinicBook.Core.DTOs.Schedules;
using ClinicBook.Core.Entities;
using ClinicBook.Core.Exceptions;
using ClinicBook.Core.Interfaces;
using ClinicBook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicBook.Infrastructure.Services;

public class DoctorScheduleService : IDoctorScheduleService
{
    private readonly ClinicBookDbContext _db;

    public DoctorScheduleService(ClinicBookDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DoctorScheduleDto>> GetForDoctorAsync(int doctorId)
    {
        await EnsureDoctorExistsAsync(doctorId);

        return await _db.DoctorSchedules
            .AsNoTracking()
            .Where(s => s.DoctorId == doctorId)
            .OrderBy(s => s.DayOfWeek)
            .Select(s => new DoctorScheduleDto(
                s.Id, s.DoctorId, s.DayOfWeek, s.StartTime, s.EndTime, s.SlotMinutes))
            .ToListAsync();
    }

    public async Task<DoctorScheduleDto> AddAsync(int doctorId, CreateDoctorScheduleRequest request)
    {
        await EnsureDoctorExistsAsync(doctorId);

        var schedule = new DoctorSchedule
        {
            DoctorId = doctorId,
            DayOfWeek = request.DayOfWeek,
            // Model validation guarantees these are present before we get here.
            StartTime = request.StartTime!.Value,
            EndTime = request.EndTime!.Value,
            SlotMinutes = request.SlotMinutes
        };

        // The entity checks its own rules (end after start, sensible slot length).
        schedule.Validate();

        // One working window per weekday, matching the unique index on (DoctorId, DayOfWeek).
        if (await _db.DoctorSchedules.AnyAsync(s => s.DoctorId == doctorId && s.DayOfWeek == request.DayOfWeek))
        {
            throw new BusinessRuleException(
                $"This doctor already has a schedule for {request.DayOfWeek}. Delete it first to replace it.");
        }

        _db.DoctorSchedules.Add(schedule);
        await _db.SaveChangesAsync();

        return new DoctorScheduleDto(
            schedule.Id, schedule.DoctorId, schedule.DayOfWeek,
            schedule.StartTime, schedule.EndTime, schedule.SlotMinutes);
    }

    public async Task DeleteAsync(int doctorId, int scheduleId)
    {
        var schedule = await _db.DoctorSchedules
            .FirstOrDefaultAsync(s => s.Id == scheduleId && s.DoctorId == doctorId)
            ?? throw NotFoundException.For("Doctor schedule", scheduleId);

        // Removing a working window only stops future bookings;
        // appointments that were already made stay untouched.
        _db.DoctorSchedules.Remove(schedule);
        await _db.SaveChangesAsync();
    }

    private async Task EnsureDoctorExistsAsync(int doctorId)
    {
        if (!await _db.Doctors.AnyAsync(d => d.Id == doctorId))
        {
            throw NotFoundException.For("Doctor", doctorId);
        }
    }
}
