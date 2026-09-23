using ClinicBook.Core.DTOs.Schedules;

namespace ClinicBook.Core.Interfaces;

public interface IDoctorScheduleService
{
    Task<IReadOnlyList<DoctorScheduleDto>> GetForDoctorAsync(int doctorId);

    Task<DoctorScheduleDto> AddAsync(int doctorId, CreateDoctorScheduleRequest request);

    Task DeleteAsync(int doctorId, int scheduleId);
}
