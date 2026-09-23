using ClinicBook.Core.DTOs.Doctors;

namespace ClinicBook.Core.Interfaces;

public interface IDoctorService
{
    /// <summary>Lists doctors, optionally filtered by specialty. Only active doctors unless asked otherwise.</summary>
    Task<IReadOnlyList<DoctorDto>> GetAllAsync(int? specialtyId = null, bool includeInactive = false);

    Task<DoctorDto> GetByIdAsync(int id);

    Task<DoctorDto> CreateAsync(CreateDoctorRequest request);

    Task<DoctorDto> UpdateAsync(int id, UpdateDoctorRequest request);

    /// <summary>
    /// The doctor profile id belonging to a login account, or null if that user is not a doctor.
    /// Used to check that a doctor is only editing their own schedule.
    /// </summary>
    Task<int?> GetDoctorIdForUserAsync(string userId);
}
