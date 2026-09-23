namespace ClinicBook.Core.Exceptions;

/// <summary>
/// Thrown when a requested record does not exist. The API translates this into a 404 response.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public static NotFoundException For(string entityName, int id) =>
        new($"{entityName} with id {id} was not found.");
}
