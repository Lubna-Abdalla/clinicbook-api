using ClinicBook.Core.Constants;
using ClinicBook.Core.DTOs.Appointments;
using ClinicBook.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBook.Api.Controllers;

/// <summary>Booking, viewing and closing appointments. Every endpoint needs a token.</summary>
[Authorize]
public class AppointmentsController : ApiControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    /// <summary>Books a free slot for the signed-in patient.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Patient)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> Book(CreateAppointmentRequest request)
    {
        var appointment = await _appointmentService.BookAsync(CurrentUserId, request);

        return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
    }

    /// <summary>The caller's own appointments: their bookings as a patient, or their agenda as a doctor.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetMine()
    {
        return Ok(await _appointmentService.GetForUserAsync(CurrentUserId));
    }

    /// <summary>One appointment. Only its patient, its doctor or an admin may read it.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> GetById(int id)
    {
        return Ok(await _appointmentService.GetByIdAsync(id, CurrentUserId, IsAdmin));
    }

    /// <summary>
    /// Cancels an appointment. The row is kept and only its status changes,
    /// which both preserves history and frees the slot for someone else.
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<AppointmentDto>> Cancel(int id)
    {
        return Ok(await _appointmentService.CancelAsync(id, CurrentUserId, IsAdmin));
    }

    /// <summary>Marks the visit as done and stores the doctor's notes. The treating doctor only.</summary>
    [HttpPost("{id:int}/complete")]
    [Authorize(Roles = AppRoles.Doctor)]
    public async Task<ActionResult<AppointmentDto>> Complete(int id, CompleteAppointmentRequest request)
    {
        return Ok(await _appointmentService.CompleteAsync(id, CurrentUserId, request));
    }

    /// <summary>Marks that the patient did not turn up. The treating doctor only.</summary>
    [HttpPost("{id:int}/no-show")]
    [Authorize(Roles = AppRoles.Doctor)]
    public async Task<ActionResult<AppointmentDto>> MarkNoShow(int id)
    {
        return Ok(await _appointmentService.MarkAsNoShowAsync(id, CurrentUserId));
    }
}
