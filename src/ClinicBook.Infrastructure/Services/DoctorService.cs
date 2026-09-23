using ClinicBook.Core.Constants;
using ClinicBook.Core.DTOs.Doctors;
using ClinicBook.Core.Entities;
using ClinicBook.Core.Exceptions;
using ClinicBook.Core.Interfaces;
using ClinicBook.Infrastructure.Data;
using ClinicBook.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicBook.Infrastructure.Services;

public class DoctorService : IDoctorService
{
    private readonly ClinicBookDbContext _db;
    private readonly UserAccountCreator _accountCreator;

    public DoctorService(ClinicBookDbContext db, UserAccountCreator accountCreator)
    {
        _db = db;
        _accountCreator = accountCreator;
    }

    public async Task<IReadOnlyList<DoctorDto>> GetAllAsync(int? specialtyId = null, bool includeInactive = false)
    {
        // Filters are applied to the entity query first; the projection to a DTO comes last,
        // because SQL cannot filter on something that only exists after the SELECT.
        var doctors = _db.Doctors.AsNoTracking();

        if (specialtyId.HasValue)
        {
            doctors = doctors.Where(d => d.SpecialtyId == specialtyId.Value);
        }

        if (!includeInactive)
        {
            doctors = doctors.Where(d => d.IsActive);
        }

        return await ProjectDoctors(doctors).ToListAsync();
    }

    public async Task<DoctorDto> GetByIdAsync(int id)
    {
        var doctors = _db.Doctors.AsNoTracking().Where(d => d.Id == id);

        return await ProjectDoctors(doctors).FirstOrDefaultAsync()
            ?? throw NotFoundException.For("Doctor", id);
    }

    public async Task<DoctorDto> CreateAsync(CreateDoctorRequest request)
    {
        await EnsureSpecialtyExistsAsync(request.SpecialtyId);

        // Both the login account and the doctor profile, or neither.
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var user = await _accountCreator.CreateAsync(
            request.FullName, request.Email, request.Password, AppRoles.Doctor);

        var doctor = new Doctor
        {
            UserId = user.Id,
            SpecialtyId = request.SpecialtyId,
            Bio = request.Bio,
            ConsultationFee = request.ConsultationFee,
            IsActive = true
        };

        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetByIdAsync(doctor.Id);
    }

    public async Task<DoctorDto> UpdateAsync(int id, UpdateDoctorRequest request)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == id)
            ?? throw NotFoundException.For("Doctor", id);

        await EnsureSpecialtyExistsAsync(request.SpecialtyId);

        doctor.SpecialtyId = request.SpecialtyId;
        doctor.Bio = request.Bio;
        doctor.ConsultationFee = request.ConsultationFee;
        doctor.IsActive = request.IsActive;

        // EF Core tracked the entity when we loaded it, so SaveChanges writes only the changed columns.
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<int?> GetDoctorIdForUserAsync(string userId)
    {
        return await _db.Doctors
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .Select(d => (int?)d.Id)
            .FirstOrDefaultAsync();
    }

    private async Task EnsureSpecialtyExistsAsync(int specialtyId)
    {
        if (!await _db.Specialties.AnyAsync(s => s.Id == specialtyId))
        {
            throw NotFoundException.For("Specialty", specialtyId);
        }
    }

    /// <summary>
    /// A doctor's name and email live in the Identity user table, so the query joins the two.
    /// Projecting straight into a DTO means SQL Server only returns the columns we actually use.
    /// </summary>
    private IQueryable<DoctorDto> ProjectDoctors(IQueryable<Doctor> doctors)
    {
        return from doctor in doctors
               join user in _db.Users on doctor.UserId equals user.Id
               orderby user.FullName
               select new DoctorDto(
                   doctor.Id,
                   user.FullName,
                   user.Email!,
                   doctor.SpecialtyId,
                   doctor.Specialty!.Name,
                   doctor.Bio,
                   doctor.ConsultationFee,
                   doctor.IsActive);
    }
}
