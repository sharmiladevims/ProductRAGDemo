using Microsoft.Extensions.Options;
using ProductRAGDemo.Configurations;
using ProductRAGDemo.Models;

namespace ProductRAGDemo.Services;

public sealed class DocumentKnowledgeService
    : IDocumentKnowledgeService
{
    private readonly List<KnowledgeDocument> _documents;

    public DocumentKnowledgeService(
        IOptions<KnowledgeSettings> options)
    {
        _documents =
            KnowledgeLoader.LoadDocuments(
                options.Value.Folder);
    }

    public List<KnowledgeDocument>
        GetDocuments()
    {
        return _documents;
    }
}