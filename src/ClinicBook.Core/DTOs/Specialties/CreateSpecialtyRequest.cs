using System.ComponentModel.DataAnnotations;

namespace ClinicBook.Core.DTOs.Specialties;

public class CreateSpecialtyRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}
