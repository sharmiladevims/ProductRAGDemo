namespace ProductRAGDemo.Models;

public sealed class KnowledgeDocument
{
    public string FileName { get; set; }
        = string.Empty;

    public string FileHash { get; set; }
    = string.Empty;

    public string Content { get; set; }
        = string.Empty;
}