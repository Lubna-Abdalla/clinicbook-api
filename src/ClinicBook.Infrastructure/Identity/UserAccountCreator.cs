using ClinicBook.Core.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace ClinicBook.Infrastructure.Identity;

/// <summary>
/// Creates an Identity login account and assigns its role.
/// Kept in one small class because two services need it: patient self-registration
/// (AuthService) and an admin creating a doctor (DoctorService).
/// </summary>
public class UserAccountCreator
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAccountCreator(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ApplicationUser> CreateAsync(string fullName, string email, string password, string role)
    {
        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            throw new BusinessRuleException("An account with this email address already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            // Identity reports problems such as a too-weak password as a list of errors.
            throw new BusinessRuleException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        await _userManager.AddToRoleAsync(user, role);

        return user;
    }
}
