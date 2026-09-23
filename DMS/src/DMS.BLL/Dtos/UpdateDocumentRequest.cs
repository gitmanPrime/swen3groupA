namespace DMS.BLL.Dtos;

/// <summary>
/// This is for PATCH /api/documents/{id}
/// Only properties that are set are applied; null means "leave unchanged"
/// Pass an empty "collectionIds" array to remove the document from all collections
/// </summary>
public class UpdateDocumentRequest
{
    public string? Description { get; init; }

    public IReadOnlyList<Guid>? CollectionIds { get; init; }
}