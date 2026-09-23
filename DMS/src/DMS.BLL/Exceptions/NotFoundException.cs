namespace DMS.BLL.Exceptions;

/// <summary>
/// Thrown when a requested resource does not exist
/// Mapped to HTTP 404
/// </summary>
public class NotFoundException : Exception
{
    public string ResourceType { get; }

    public Guid ResourceId { get; }

    public NotFoundException(string resourceType, Guid resourceId) : base($"{resourceType} with id '{resourceId}' was not found.")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public NotFoundException(string message) : base(message)
    {
        ResourceType = string.Empty;
    }
}