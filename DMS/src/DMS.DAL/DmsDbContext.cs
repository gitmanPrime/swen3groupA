using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL;

public class DmsDbContext(DbContextOptions<DmsDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<Collection> Collections => Set<Collection>();

    public DbSet<DocumentTag> DocumentTags => Set<DocumentTag>();

    public DbSet<DocumentCollection> DocumentCollections => Set<DocumentCollection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DmsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}