using DMS.BLL.Dtos;

namespace DMS.BLL.Interfaces.Services;

public interface ICollectionService
{
    // create a collection
    Task<CollectionDto> CreateAsync(string name, string? description = null, CancellationToken cancellationToken = default);
    // list collections
    Task<IReadOnlyList<CollectionDto>> ListAsync(CancellationToken cancellationToken = default);
    // get collection details (by id)
    Task<CollectionDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    // rename a collection (by id)
    Task<CollectionDto> RenameAsync(Guid id, string name, CancellationToken cancellationToken = default);
    // delete a collection (by id)
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}