namespace ClinicBook.Core.Exceptions;

/// <summary>
/// Thrown when a request is well-formed but breaks a clinic rule
/// (for example booking a slot that is already taken).
/// The API translates this into a 409 Conflict response.
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}
