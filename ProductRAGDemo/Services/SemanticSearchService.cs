
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

using Pgvector;
using Pgvector.EntityFrameworkCore;

using ProductRAGDemo.Data;

namespace ProductRAGDemo.Services;

public sealed class SemanticSearchService
{
    private readonly ProductDbContext _db;
    private readonly EmbeddingService _embeddingService;
    private readonly LlmService _llmService;
    private readonly IDistributedCache _cache;

    // Change this version when indexed documents or cache logic changes.
    private const string CacheVersion = "v1";

    public SemanticSearchService(
        ProductDbContext db,
        EmbeddingService embeddingService,
        LlmService llmService,
        IDistributedCache cache)
    {
        _db = db;
        _embeddingService = embeddingService;
        _llmService = llmService;
        _cache = cache;
    }

    public async Task SearchAsync(string question)
    {
        var normalizedQuestion = question.Trim().ToLowerInvariant();

        var answerCacheKey =
            $"answer:{CacheVersion}:{GenerateHash(normalizedQuestion)}";

        var cacheLogs = new List<string>();

        // STEP 1: Check answer cache
        var cachedAnswer =
            await _cache.GetStringAsync(answerCacheKey);

        if (!string.IsNullOrEmpty(cachedAnswer))
        {
            Console.WriteLine();
            Console.WriteLine("[ANSWER CACHE HIT]");
            Console.WriteLine("Returning answer directly from Garnet.");
            Console.WriteLine();
            Console.WriteLine($"Answer: {cachedAnswer}");
            Console.WriteLine();

            return;
        }
        else
        {
            Console.WriteLine("[ANSWER CACHE MISS]");
            Console.WriteLine("Returning answer directly from [VECTOR SEARCH].");
        }

        cacheLogs.Add("[ANSWER CACHE MISS]");
        cacheLogs.Add("Answer not found in Garnet.");

        string answer = string.Empty;

        // STEP 2: Execute RAG pipeline
        await RunWithLoaderAsync(async () =>
        {
            // Generate or retrieve embedding
            var embedding = await GetEmbeddingAsync(
                normalizedQuestion,
                cacheLogs);

            var vector = new Vector(embedding);

            // STEP 3: PostgreSQL semantic search
            cacheLogs.Add("[VECTOR SEARCH]");
            cacheLogs.Add("Searching PostgreSQL using pgvector.");

            var matches = await _db.Chunks
                .OrderBy(x =>
                    x.Embedding.CosineDistance(vector))
                .Take(3)
                .ToListAsync();

            // STEP 4: Prepare context
            var context = string.Join(
                Environment.NewLine,
                matches.Select(x => x.Content));

            // STEP 5: Generate AI answer
            cacheLogs.Add("[OLLAMA]");
            cacheLogs.Add("Generating answer using Ollama.");

            answer = await _llmService.Ask(
                question,
                context);

            // STEP 6: Save answer in Garnet
            await _cache.SetStringAsync(
                answerCacheKey,
                answer,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMinutes(30)
                });

            cacheLogs.Add("[ANSWER CACHE SET]");
            cacheLogs.Add("Answer saved in Garnet. Expiry: 30 minutes.");
        });

        // Display cache and processing logs
        Console.WriteLine();

        foreach (var log in cacheLogs)
        {
            //Console.WriteLine(log);
        }

        Console.WriteLine();
        Console.WriteLine($"Answer: {answer}");
        Console.WriteLine();
    }

    // ==========================================
    // EMBEDDING CACHE
    // ==========================================

    private async Task<float[]> GetEmbeddingAsync(
        string question,
        List<string> cacheLogs)
    {
        var embeddingCacheKey =
            $"embedding:{CacheVersion}:{GenerateHash(question)}";

        var cachedEmbedding =
            await _cache.GetStringAsync(embeddingCacheKey);

        if (!string.IsNullOrEmpty(cachedEmbedding))
        {
            try
            {
                var embedding =
                    JsonSerializer.Deserialize<float[]>(cachedEmbedding);

                if (embedding is { Length: > 0 })
                {
                    cacheLogs.Add("[EMBEDDING CACHE HIT]");
                    cacheLogs.Add("Embedding retrieved from Garnet.");

                    return embedding;
                }
            }
            catch (JsonException)
            {
                cacheLogs.Add(
                    "[EMBEDDING CACHE INVALID] Regenerating embedding.");
            }
        }

        cacheLogs.Add("[EMBEDDING CACHE MISS]");
        cacheLogs.Add("Generating embedding using Ollama.");

        var generatedEmbedding =
            await _embeddingService.CreateEmbedding(question);

        await _cache.SetStringAsync(
            embeddingCacheKey,
            JsonSerializer.Serialize(generatedEmbedding),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromHours(24)
            });

        cacheLogs.Add("[EMBEDDING CACHE SET]");
        cacheLogs.Add("Embedding saved in Garnet. Expiry: 24 hours.");

        return generatedEmbedding;
    }

    // ==========================================
    // SHA256 CACHE KEY
    // ==========================================

    private static string GenerateHash(string value)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(value));

        return Convert.ToHexString(bytes);
    }

    // ==========================================
    // CONSOLE LOADER
    // ==========================================

    private static async Task RunWithLoaderAsync(
        Func<Task> action)
    {
        var spinner = new[] { '|', '/', '-', '\\' };

        using var cts = new CancellationTokenSource();

        var loaderTask = Task.Run(async () =>
        {
            var counter = 0;

            try
            {
                while (true)
                {
                    Console.Write(
                        $"\rAnswer: Processing {spinner[counter++ % spinner.Length]}");

                    await Task.Delay(100, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when processing completes.
            }
        });

        try
        {
            await action();
        }
        finally
        {
            cts.Cancel();

            await loaderTask;

            Console.Write(
                "\r" + new string(' ', 60) + "\r");
        }
    }
}
