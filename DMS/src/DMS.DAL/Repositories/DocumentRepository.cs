using DMS.BLL.Interfaces.Repositories;
using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class DocumentRepository(DmsDbContext dbContext) : IDocumentRepository
{
    private IQueryable<Document> BaseQuery => dbContext.Documents
        .Include(d => d.DocumentTags)
        .ThenInclude(dt => dt.Tag)
        .Include(d => d.DocumentCollections)
        .ThenInclude(dc => dc.Collection);

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await BaseQuery.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetAllAsync(Guid? collectionId = null, Guid? tagId = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Document> query = BaseQuery;

        if (collectionId.HasValue)
            query = query.Where(d => d.DocumentCollections.Any(dc => dc.CollectionId == collectionId.Value));

        if (tagId.HasValue)
            query = query.Where(d => d.DocumentTags.Any(dt => dt.TagId == tagId.Value));

        return await query.OrderByDescending(d => d.UploadedAt).AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        await dbContext.Documents.AddAsync(document, cancellationToken);
    }

    public void Update(Document document)
    {
        dbContext.Documents.Update(document);
    }

    public void Remove(Document document)
    {
        dbContext.Documents.Remove(document);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}