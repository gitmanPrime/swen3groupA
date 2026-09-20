namespace DMS.Domain.Entities;

public class Document
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
}