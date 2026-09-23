namespace DMS.BLL.Exceptions;

/// <summary>
/// Thrown when an operation conflicts with the current state of the system
/// Mapped to HTTP 409
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}