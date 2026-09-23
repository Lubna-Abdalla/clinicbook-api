using System.Security.Claims;
using ClinicBook.Core.Constants;
using ClinicBook.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBook.Api.Controllers;

/// <summary>
/// Shared plumbing for the controllers: the route pattern and a couple of helpers
/// for reading the signed-in user out of the JWT claims.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>The Identity user id that was put into the token when the user logged in.</summary>
    protected string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedException("The token does not contain a user id.");

    protected bool IsAdmin => User.IsInRole(AppRoles.Admin);
}
