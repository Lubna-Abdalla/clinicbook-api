namespace ClinicBook.Core.Exceptions;

/// <summary>
/// Thrown when the caller is logged in but is not allowed to touch this particular record,
/// for example a patient trying to cancel someone else's appointment.
/// The API translates this into a 403 Forbidden response.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
