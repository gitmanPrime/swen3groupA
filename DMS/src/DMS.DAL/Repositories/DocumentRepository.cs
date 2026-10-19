using DMS.BLL.Exceptions;
using DMS.BLL.Interfaces.Repositories;
using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

    public async Task SaveChangesAsync(
    CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
                    or PostgresErrorCodes.ForeignKeyViolation
            })
        {
            throw new ConflictException(
                "The operation conflicts with existing or related data.",
                ex);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException(
                "An unexpected error occurred while saving database changes.",
                ex);
        }
    }
}