using ClinicBook.Core.Constants;
using ClinicBook.Core.DTOs.Specialties;
using ClinicBook.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBook.Api.Controllers;

public class SpecialtiesController : ApiControllerBase
{
    private readonly ISpecialtyService _specialtyService;

    public SpecialtiesController(ISpecialtyService specialtyService)
    {
        _specialtyService = specialtyService;
    }

    /// <summary>Lists all specialties. Public, so a visitor can browse before signing up.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<SpecialtyDto>>> GetAll()
    {
        return Ok(await _specialtyService.GetAllAsync());
    }

    /// <summary>Adds a specialty. Admins only.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SpecialtyDto>> Create(CreateSpecialtyRequest request)
    {
        var specialty = await _specialtyService.CreateAsync(request);

        // 201 Created with a Location header pointing at the list endpoint.
        return CreatedAtAction(nameof(GetAll), new { id = specialty.Id }, specialty);
    }
}
