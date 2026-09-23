using Microsoft.AspNetCore.Identity;

namespace ClinicBook.Infrastructure.Identity;

/// <summary>
/// The application's user account, stored in the AspNetUsers table.
/// It inherits everything ASP.NET Core Identity needs (Id, Email, PasswordHash, security stamps...)
/// and adds the two extra columns this project needs.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
