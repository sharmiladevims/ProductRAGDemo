using Pgvector;

namespace ProductRAGDemo.Models;

public sealed class DocumentChunk
{
    public int Id { get; set; }

    public string SourceFileName { get; set; }
        = string.Empty;

    public string SourceFileHash { get; set; }
        = string.Empty;

    public string Content { get; set; }
        = string.Empty;

    public Vector Embedding { get; set; }
        = null!;

    public DateTime IndexedDate { get; set; }
}
