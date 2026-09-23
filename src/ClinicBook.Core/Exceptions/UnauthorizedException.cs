namespace ClinicBook.Core.Exceptions;

/// <summary>
/// Thrown when sign-in fails (wrong email or password).
/// The API translates this into a 401 Unauthorized response.
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}
