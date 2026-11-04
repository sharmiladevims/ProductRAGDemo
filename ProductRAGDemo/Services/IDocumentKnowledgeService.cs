using ProductRAGDemo.Models;

namespace ProductRAGDemo.Services;

public interface IDocumentKnowledgeService
{
    List<KnowledgeDocument> GetDocuments();
}