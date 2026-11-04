using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProductRAGDemo.Configurations;

namespace ProductRAGDemo.Services;

public sealed class LlmService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;

    public LlmService(
        IOptions<OllamaSettings> options)
    {
        _settings = options.Value;

        _httpClient = new HttpClient
        {
            BaseAddress =
                new Uri(_settings.BaseUrl)
        };
    }

    public async Task<string> Ask(
        string question,
        string context)
    {
        var prompt = $"""
            Answer using only the supplied context.

            Context:
            {context}

            Question:
            {question}

            Answer:
            """;

        var request = new
        {
            model = _settings.ChatModel,
            prompt,
            stream = false
        };

        var response =
            await _httpClient.PostAsync(
                "/api/generate",
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
            .GetProperty("response")
            .GetString()
            ?? string.Empty;
    }
}