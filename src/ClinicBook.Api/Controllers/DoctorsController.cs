using ClinicBook.Core.Constants;
using ClinicBook.Core.DTOs.Appointments;
using ClinicBook.Core.DTOs.Doctors;
using ClinicBook.Core.DTOs.Schedules;
using ClinicBook.Core.Exceptions;
using ClinicBook.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBook.Api.Controllers;

public class DoctorsController : ApiControllerBase
{
    private readonly IDoctorService _doctorService;
    private readonly IDoctorScheduleService _scheduleService;
    private readonly IAppointmentService _appointmentService;

    public DoctorsController(
        IDoctorService doctorService,
        IDoctorScheduleService scheduleService,
        IAppointmentService appointmentService)
    {
        _doctorService = doctorService;
        _scheduleService = scheduleService;
        _appointmentService = appointmentService;
    }

    /// <summary>Lists doctors, optionally filtered by specialty. Inactive doctors are admin-only.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<DoctorDto>>> GetAll(
        [FromQuery] int? specialtyId,
        [FromQuery] bool includeInactive = false)
    {
        return Ok(await _doctorService.GetAllAsync(specialtyId, includeInactive && IsAdmin));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoctorDto>> GetById(int id)
    {
        return Ok(await _doctorService.GetByIdAsync(id));
    }

    /// <summary>Creates a doctor: this also creates their login account. Admins only.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<DoctorDto>> Create(CreateDoctorRequest request)
    {
        var doctor = await _doctorService.CreateAsync(request);

        return CreatedAtAction(nameof(GetById), new { id = doctor.Id }, doctor);
    }

    /// <summary>Updates the clinic data of a doctor (fee, bio, specialty, active flag). Admins only.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<DoctorDto>> Update(int id, UpdateDoctorRequest request)
    {
        return Ok(await _doctorService.UpdateAsync(id, request));
    }

    /// <summary>The doctor's recurring weekly working hours.</summary>
    [HttpGet("{id:int}/schedules")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<DoctorScheduleDto>>> GetSchedules(int id)
    {
        return Ok(await _scheduleService.GetForDoctorAsync(id));
    }

    /// <summary>Adds one weekly working window. An admin, or the doctor themselves.</summary>
    [HttpPost("{id:int}/schedules")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Doctor}")]
    public async Task<ActionResult<DoctorScheduleDto>> AddSchedule(int id, CreateDoctorScheduleRequest request)
    {
        await EnsureCanManageScheduleAsync(id);

        var schedule = await _scheduleService.AddAsync(id, request);

        return CreatedAtAction(nameof(GetSchedules), new { id }, schedule);
    }

    /// <summary>Removes one weekly working window. Existing appointments are not affected.</summary>
    [HttpDelete("{id:int}/schedules/{scheduleId:int}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Doctor}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSchedule(int id, int scheduleId)
    {
        await EnsureCanManageScheduleAsync(id);
        await _scheduleService.DeleteAsync(id, scheduleId);

        return NoContent();
    }

    /// <summary>The slot start times this doctor still has free on the given date.</summary>
    [HttpGet("{id:int}/available-slots")]
    [AllowAnonymous]
    public async Task<ActionResult<AvailableSlotsResponse>> GetAvailableSlots(
        int id,
        [FromQuery] DateOnly date)
    {
        return Ok(await _appointmentService.GetAvailableSlotsAsync(id, date));
    }

    /// <summary>
    /// Role checks alone are not enough here: a doctor may only touch their OWN schedule,
    /// so we compare the doctor profile of the signed-in user with the id in the route.
    /// </summary>
    private async Task EnsureCanManageScheduleAsync(int doctorId)
    {
        if (IsAdmin)
        {
            return;
        }

        var ownDoctorId = await _doctorService.GetDoctorIdForUserAsync(CurrentUserId);

        if (ownDoctorId != doctorId)
        {
            throw new ForbiddenException("You can only manage your own schedule.");
        }
    }
}
