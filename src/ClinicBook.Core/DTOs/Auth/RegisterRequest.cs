using System.ComponentModel.DataAnnotations;
using ClinicBook.Core.Validators;

namespace ClinicBook.Core.DTOs.Auth;

/// <summary>
/// Patient self-registration. The attributes are checked by ASP.NET Core model validation
/// before the controller action runs, so bad input never reaches the business logic.
/// </summary>
public class RegisterRequest
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    // Nullable on purpose: [Required] only rejects null, so a non-nullable DateOnly would
    // silently accept a missing value as 0001-01-01 instead of returning 400.
    [Required]
    [PastDate]
    public DateOnly? DateOfBirth { get; set; }

    [Required]
    [Phone]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
}
