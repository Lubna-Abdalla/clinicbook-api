using System.ComponentModel.DataAnnotations;

namespace ClinicBook.Core.DTOs.Doctors;

public class UpdateDoctorRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid specialty must be selected.")]
    public int SpecialtyId { get; set; }

    [StringLength(1000)]
    public string? Bio { get; set; }

    [Range(0, 99_999_999.99, ErrorMessage = "Consultation fee must be zero or more.")]
    public decimal ConsultationFee { get; set; }

    public bool IsActive { get; set; }
}
