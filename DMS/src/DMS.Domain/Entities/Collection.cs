namespace DMS.Domain.Entities;

public class Collection
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // UTC timestamp of when the collection was created
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public ICollection<DocumentCollection> DocumentCollections { get; } = new List<DocumentCollection>();

    public Collection(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }

    // required for EF Core
    private Collection()
    {
    }
}