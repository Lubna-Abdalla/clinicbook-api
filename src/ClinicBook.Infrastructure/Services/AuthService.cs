using ClinicBook.Core.Constants;
using ClinicBook.Core.DTOs.Auth;
using ClinicBook.Core.Entities;
using ClinicBook.Core.Exceptions;
using ClinicBook.Core.Interfaces;
using ClinicBook.Infrastructure.Data;
using ClinicBook.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace ClinicBook.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ClinicBookDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserAccountCreator _accountCreator;
    private readonly JwtTokenGenerator _tokenGenerator;

    public AuthService(
        ClinicBookDbContext db,
        UserManager<ApplicationUser> userManager,
        UserAccountCreator accountCreator,
        JwtTokenGenerator tokenGenerator)
    {
        _db = db;
        _userManager = userManager;
        _accountCreator = accountCreator;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResponse> RegisterPatientAsync(RegisterRequest request)
    {
        // A transaction so we never end up with a login account that has no patient profile:
        // either both rows are saved, or neither is.
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var user = await _accountCreator.CreateAsync(
            request.FullName, request.Email, request.Password, AppRoles.Patient);

        _db.Patients.Add(new Patient
        {
            UserId = user.Id,
            // Model validation guarantees these are present before we get here.
            DateOfBirth = request.DateOfBirth!.Value,
            PhoneNumber = request.PhoneNumber
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await BuildAuthResponseAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        // The same message for "unknown email" and "wrong password", so the API does not
        // reveal which email addresses are registered.
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return await BuildAuthResponseAsync(user);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAt) = _tokenGenerator.CreateToken(user, roles);

        return new AuthResponse(token, expiresAt, user.FullName, user.Email!, roles.ToList());
    }
}
