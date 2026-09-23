using System.ComponentModel.DataAnnotations;

namespace ClinicBook.Core.DTOs.Doctors;

/// <summary>
/// Admins create doctors: this creates both the login account and the doctor profile.
/// </summary>
public class CreateDoctorRequest
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

    [Range(1, int.MaxValue, ErrorMessage = "A valid specialty must be selected.")]
    public int SpecialtyId { get; set; }

    [StringLength(1000)]
    public string? Bio { get; set; }

    [Range(0, 99_999_999.99, ErrorMessage = "Consultation fee must be zero or more.")]
    public decimal ConsultationFee { get; set; }
}
