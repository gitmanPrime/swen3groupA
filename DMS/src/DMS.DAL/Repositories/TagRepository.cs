using DMS.BLL.Exceptions;
using DMS.BLL.Interfaces.Repositories;
using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DMS.DAL.Repositories;

public class TagRepository(DmsDbContext dbContext) : ITagRepository
{
    public async Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await dbContext.Tags.FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Tags.AsNoTracking().OrderBy(t => t.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        await dbContext.Tags.AddAsync(tag, cancellationToken);
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