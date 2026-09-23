using ClinicBook.Core.DTOs.Specialties;
using ClinicBook.Core.Entities;
using ClinicBook.Core.Exceptions;
using ClinicBook.Core.Interfaces;
using ClinicBook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicBook.Infrastructure.Services;

public class SpecialtyService : ISpecialtyService
{
    private readonly ClinicBookDbContext _db;

    public SpecialtyService(ClinicBookDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SpecialtyDto>> GetAllAsync()
    {
        // AsNoTracking: a read-only query does not need EF Core's change tracking, so it is faster.
        return await _db.Specialties
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SpecialtyDto(s.Id, s.Name))
            .ToListAsync();
    }

    public async Task<SpecialtyDto> CreateAsync(CreateSpecialtyRequest request)
    {
        var name = request.Name.Trim();

        if (await _db.Specialties.AnyAsync(s => s.Name == name))
        {
            throw new BusinessRuleException($"A specialty named '{name}' already exists.");
        }

        var specialty = new Specialty { Name = name };

        _db.Specialties.Add(specialty);
        await _db.SaveChangesAsync();

        return new SpecialtyDto(specialty.Id, specialty.Name);
    }
}
