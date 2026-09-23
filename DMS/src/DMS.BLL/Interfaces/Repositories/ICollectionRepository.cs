using DMS.Domain.Entities;

namespace DMS.BLL.Interfaces.Repositories;

public interface ICollectionRepository
{
    Task<Collection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // lists all collections (ordered by name)
    Task<IReadOnlyList<Collection>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Collection collection, CancellationToken cancellationToken = default);

    void Update(Collection collection);

    void Remove(Collection collection);

    // number of documents currently assigned to the collection
    Task<int> CountDocumentsAsync(Guid id, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}