namespace DMS.BLL.Exceptions;

/// <summary>
/// Thrown when an operation violates a business rule or input constraint
/// Mapped to HTTP 400
/// </summary>
public class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message) : this(new Dictionary<string, string[]> { [string.Empty] = [message] })
    {
    }

    public ValidationException(string propertyName, string message) : this(new Dictionary<string, string[]> { [propertyName] = [message] })
    {
    }

    public ValidationException(IDictionary<string, string[]> errors) : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>(errors);
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
        Errors = new Dictionary<string, string[]> { [string.Empty] = [message] };
    }
}