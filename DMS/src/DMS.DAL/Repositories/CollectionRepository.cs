using DMS.BLL.Interfaces.Repositories;
using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class CollectionRepository(DmsDbContext dbContext) : ICollectionRepository
{
    public async Task<Collection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Collections.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Collection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Collections.AsNoTracking().OrderBy(c => c.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Collection collection, CancellationToken cancellationToken = default)
    {
        await dbContext.Collections.AddAsync(collection, cancellationToken);
    }

    public void Update(Collection collection)
    {
        dbContext.Collections.Update(collection);
    }

    public void Remove(Collection collection)
    {
        dbContext.Collections.Remove(collection);
    }

    public Task<int> CountDocumentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.DocumentCollections.CountAsync(dc => dc.CollectionId == id, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}