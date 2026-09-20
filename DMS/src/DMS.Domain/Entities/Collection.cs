namespace DMS.Domain.Entities;

public class Collection
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}