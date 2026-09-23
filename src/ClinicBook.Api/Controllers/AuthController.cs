using System.Security.Claims;
using ClinicBook.Core.DTOs.Auth;
using ClinicBook.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBook.Api.Controllers;

/// <summary>Registration and login.</summary>
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    // Constructor injection: the DI container supplies the service, the controller just uses it.
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Registers a new patient and returns a token for them.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        return Ok(await _authService.RegisterPatientAsync(request));
    }

    /// <summary>Signs in and returns a JWT to send as "Authorization: Bearer &lt;token&gt;".</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        return Ok(await _authService.LoginAsync(request));
    }

    /// <summary>Returns who the current token belongs to. Handy for checking that auth works.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Me()
    {
        return Ok(new CurrentUserResponse(
            CurrentUserId,
            User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList()));
    }
}
