using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Configurations;

public class DocumentCollectionConfiguration : IEntityTypeConfiguration<DocumentCollection>
{
    public void Configure(EntityTypeBuilder<DocumentCollection> builder)
    {
        builder.ToTable("document_collections");

        builder.HasKey(dc => new { dc.DocumentId, dc.CollectionId });

        builder.HasOne(dc => dc.Document)
            .WithMany(d => d.DocumentCollections)
            .HasForeignKey(dc => dc.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(dc => dc.Collection)
            .WithMany(c => c.DocumentCollections)
            .HasForeignKey(dc => dc.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}