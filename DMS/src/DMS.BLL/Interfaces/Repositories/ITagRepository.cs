using DMS.Domain.Entities;

namespace DMS.BLL.Interfaces.Repositories;

public interface ITagRepository
{
    Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    // lists all tags, ordered by name
    Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Tag tag, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}