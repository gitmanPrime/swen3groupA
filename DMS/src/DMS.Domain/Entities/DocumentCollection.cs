namespace DMS.Domain.Entities;

public class DocumentCollection
{
    public Guid DocumentId { get; private set; }

    public Guid CollectionId { get; private set; }

    public DocumentCollection(Guid documentId, Guid collectionId)
    {
        DocumentId = documentId;
        CollectionId = collectionId;
    }
}