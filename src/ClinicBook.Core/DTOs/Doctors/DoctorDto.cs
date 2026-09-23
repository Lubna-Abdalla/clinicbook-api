namespace ClinicBook.Core.DTOs.Doctors;

/// <summary>
/// A doctor as the API exposes it: the clinic data joined with the name/email from the user account.
/// Using a DTO instead of the entity means we never leak password hashes or navigation loops.
/// </summary>
public record DoctorDto(
    int Id,
    string FullName,
    string Email,
    int SpecialtyId,
    string SpecialtyName,
    string? Bio,
    decimal ConsultationFee,
    bool IsActive);
