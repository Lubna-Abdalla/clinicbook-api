using ClinicBook.Core.DTOs.Auth;

namespace ClinicBook.Core.Interfaces;

/// <summary>
/// Registration and login. The interface lives in Core and the implementation in Infrastructure,
/// so the API depends on the contract, not on Identity or EF Core.
/// </summary>
public interface IAuthService
{
    Task<AuthResponse> RegisterPatientAsync(RegisterRequest request);

    Task<AuthResponse> LoginAsync(LoginRequest request);
}
