using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProductRAGDemo.Configurations;

namespace ProductRAGDemo.Services;

public sealed class EmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;

    public EmbeddingService(
        IOptions<OllamaSettings> options)
    {
        _settings = options.Value;

        _httpClient = new HttpClient
        {
            BaseAddress =
                new Uri(_settings.BaseUrl)
        };
    }

    public async Task<float[]> CreateEmbedding(
        string text)
    {
        var request = new
        {
            model = _settings.EmbeddingModel,
            prompt = text
        };

        var response =
            await _httpClient.PostAsync(
                "/api/embeddings",
                new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json"));

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content.ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(json);

        return document.RootElement
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(x => x.GetSingle())
            .ToArray();
    }
}
