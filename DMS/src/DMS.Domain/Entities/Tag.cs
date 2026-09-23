namespace DMS.Domain.Entities;

public class Tag
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public ICollection<DocumentTag> DocumentTags { get; } = new List<DocumentTag>();

    public Tag(string name)
    {
        Name = name;
    }

    // required for EF Core
    private Tag()
    {
    }
}