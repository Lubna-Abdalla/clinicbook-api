namespace ClinicBook.Core.Constants;

/// <summary>
/// The three roles the application uses. Constants instead of loose strings,
/// so a typo becomes a compile error instead of a silent authorization hole.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Doctor = "Doctor";
    public const string Patient = "Patient";

    public static readonly string[] All = [Admin, Doctor, Patient];
}
