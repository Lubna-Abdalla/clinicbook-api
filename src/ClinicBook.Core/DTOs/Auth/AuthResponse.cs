namespace ClinicBook.Core.DTOs.Auth;

/// <summary>
/// What a successful register/login returns. A record because a response is read-only data.
/// </summary>
public record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles);
