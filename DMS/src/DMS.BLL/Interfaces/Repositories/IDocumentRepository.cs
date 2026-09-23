using DMS.Domain.Entities;

namespace DMS.BLL.Interfaces.Repositories;

public interface IDocumentRepository
{
    // loads a document with its tags and collections
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // lists documents, optionally filtered by collection and/or tag membership (ordered by upload date, newest first)
    Task<IReadOnlyList<Document>> GetAllAsync(Guid? collectionId = null, Guid? tagId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Document document, CancellationToken cancellationToken = default);

    void Update(Document document);

    void Remove(Document document);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}