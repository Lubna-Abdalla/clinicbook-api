namespace ClinicBook.Infrastructure.Identity;

/// <summary>
/// The JWT values read from configuration (the "Jwt" section of User Secrets / appsettings).
/// The signing key is a secret and must never be committed to Git.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "ClinicBook";

    public string Audience { get; set; } = "ClinicBookClients";

    public int ExpiryMinutes { get; set; } = 60;
}
