namespace ProductRAGDemo.Configurations;

public sealed class OllamaSettings
{
    public string BaseUrl { get; set; }
        = string.Empty;

    public string ChatModel { get; set; }
        = string.Empty;

    public string EmbeddingModel { get; set; }
        = string.Empty;
}