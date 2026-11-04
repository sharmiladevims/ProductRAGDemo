using Microsoft.EntityFrameworkCore;
using ProductRAGDemo.Models;

namespace ProductRAGDemo.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(
        DbContextOptions<ProductDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products =>
        Set<Product>();

    public DbSet<DocumentChunk> Chunks =>
        Set<DocumentChunk>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DocumentChunk>(
        entity =>
        {
            entity.ToTable("document_chunks");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id");

            entity.Property(x => x.SourceFileName)
                .HasColumnName("source_file_name");

            entity.Property(x => x.SourceFileHash)
                .HasColumnName("source_file_hash");

            entity.Property(x => x.Content)
                .HasColumnName("content");

            entity.Property(x => x.Embedding)
                .HasColumnName("embedding")
                .HasColumnType("vector(768)");

            entity.Property(x => x.IndexedDate)
                .HasColumnName("indexed_date");
        });
    }
}