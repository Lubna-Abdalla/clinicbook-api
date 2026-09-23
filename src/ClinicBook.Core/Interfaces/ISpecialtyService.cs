using ClinicBook.Core.DTOs.Specialties;

namespace ClinicBook.Core.Interfaces;

public interface ISpecialtyService
{
    Task<IReadOnlyList<SpecialtyDto>> GetAllAsync();

    Task<SpecialtyDto> CreateAsync(CreateSpecialtyRequest request);
}
