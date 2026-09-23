namespace DMS.Domain.Entities;

/// <summary>
/// Join entity for the many-to-many relationship between documents and tags.
/// The pair (DocumentId, TagId) is the composite primary key.
/// </summary>
public class DocumentTag
{
    public Guid DocumentId { get; private set; }

    public Guid TagId { get; private set; }

    public Document Document { get; set; } = null!;

    public Tag Tag { get; set; } = null!;

    public DocumentTag(Guid documentId, Guid tagId)
    {
        DocumentId = documentId;
        TagId = tagId;
    }

    // required for EF Core
    private DocumentTag()
    {
    }
}