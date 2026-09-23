using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.DAL.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(d => d.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(d => d.FileSize)
            .HasColumnName("file_size");

        builder.Property(d => d.UploadedAt)
            .HasColumnName("uploaded_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(d => d.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(d => d.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(512)
            .IsRequired();
    }
}