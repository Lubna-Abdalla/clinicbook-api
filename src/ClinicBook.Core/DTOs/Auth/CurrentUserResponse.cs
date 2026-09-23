namespace ClinicBook.Core.DTOs.Auth;

/// <summary>
/// Who the current token belongs to, read from the JWT claims.
/// </summary>
public record CurrentUserResponse(
    string Id,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles);
