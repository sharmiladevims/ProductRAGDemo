using Microsoft.EntityFrameworkCore;
using Pgvector;
using ProductRAGDemo.Data;
using ProductRAGDemo.Models;

namespace ProductRAGDemo.Services;

public sealed class DocumentIndexerService
{
    private readonly ProductDbContext _db;
    private readonly EmbeddingService _embeddingService;
    private readonly IDocumentKnowledgeService _knowledge;

    public DocumentIndexerService(
        ProductDbContext db,
        EmbeddingService embeddingService,
        IDocumentKnowledgeService knowledge)
    {
        _db = db;
        _embeddingService = embeddingService;
        _knowledge = knowledge;
    }

    public async Task IndexAsync()
    {
        var documents =
            _knowledge.GetDocuments();

        Console.WriteLine();
        Console.WriteLine(
            $"Documents Found : {documents.Count}");

        foreach (var document in documents)
        {
            var existingHash =
                await _db.Chunks
                    .Where(x =>
                        x.SourceFileName ==
                        document.FileName)
                    .Select(x =>
                        x.SourceFileHash)
                    .FirstOrDefaultAsync();

            //
            // Skip if same file + same content
            //
            if (!string.IsNullOrWhiteSpace(existingHash) &&
                existingHash == document.FileHash)
            {
                continue;
            }

            //
            // File changed -> remove old chunks
            //
            if (!string.IsNullOrWhiteSpace(existingHash))
            {
                Console.WriteLine(
                    $"Reindexing {document.FileName}");

                await _db.Chunks
                    .Where(x =>
                        x.SourceFileName ==
                        document.FileName)
                    .ExecuteDeleteAsync();
            }
            else
            {
                Console.WriteLine(
                    $"Indexing {document.FileName}");
            }

            var chunks =
                TextChunker.Chunk(
                    document.Content);

            Console.WriteLine(
                $"Chunks Created : {chunks.Count}");

            foreach (var chunk in chunks)
            {
                var embedding =
                    await _embeddingService
                        .CreateEmbedding(chunk);

                Console.WriteLine(
                    $"Embedding Size : {embedding.Length}");

                _db.Chunks.Add(
                    new DocumentChunk
                    {
                        SourceFileName =
                            document.FileName,

                        SourceFileHash =
                            document.FileHash,

                        Content =
                            chunk,

                        Embedding =
                            new Vector(embedding),

                        IndexedDate =
                            DateTime.UtcNow
                    });
            }
        }

        await _db.SaveChangesAsync();

        Console.WriteLine();
        Console.WriteLine(
            "Document indexing completed.");
    }
}