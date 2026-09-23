namespace DMS.Domain.Entities;

public class DocumentCollection
{
    public Guid DocumentId { get; private set; }

    public Guid CollectionId { get; private set; }

    public Document Document { get; set; } = null!;

    public Collection Collection { get; set; } = null!;

    public DocumentCollection(Guid documentId, Guid collectionId)
    {
        DocumentId = documentId;
        CollectionId = collectionId;
    }

    // required for EF Core
    private DocumentCollection()
    {
    }
}