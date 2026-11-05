
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProductRAGDemo.Configurations;
using ProductRAGDemo.Data;
using ProductRAGDemo.Services;

var builder = Host.CreateApplicationBuilder(args);

// Hide EF Core SQL query logs
builder.Logging.AddFilter(
    "Microsoft.EntityFrameworkCore.Database.Command",
    LogLevel.Warning);

// Configuration
builder.Services.Configure<OllamaSettings>(
    builder.Configuration.GetSection("OllamaSettings"));

builder.Services.Configure<KnowledgeSettings>(
    builder.Configuration.GetSection("KnowledgeSettings"));

// PostgreSQL Database
var connectionString =
    builder.Configuration["DatabaseSettings:DefaultConnection"]
    ?? throw new Exception(
        "DatabaseSettings:DefaultConnection missing.");

builder.Services.AddDbContext<ProductDbContext>(
    options =>
    {
        options.UseNpgsql(
            connectionString,
            x => x.UseVector());
    });

// Garnet / Redis Distributed Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration =
        builder.Configuration["Redis:ConnectionString"]
        ?? "localhost:6379,abortConnect=false";

    options.InstanceName = "ProductRAGDemo:";
});

// Application Services
builder.Services.AddSingleton<
    IDocumentKnowledgeService,
    DocumentKnowledgeService>();

builder.Services.AddSingleton<EmbeddingService>();

builder.Services.AddSingleton<LlmService>();

builder.Services.AddTransient<DocumentIndexerService>();

builder.Services.AddScoped<SemanticSearchService>();

// Build Host
var host = builder.Build();

using var scope = host.Services.CreateScope();

var serviceProvider = scope.ServiceProvider;

// Initial Document Indexing
var documentIndexer =
    serviceProvider.GetRequiredService<DocumentIndexerService>();

Console.WriteLine();
Console.WriteLine("====================================");
Console.WriteLine("          PRODUCT RAG");
Console.WriteLine("====================================");
Console.WriteLine();

await documentIndexer.IndexAsync();

// Start RAG
var semanticSearchService =
    serviceProvider.GetRequiredService<SemanticSearchService>();

Console.WriteLine();
Console.WriteLine("====================================");
Console.WriteLine("          Product RAG Ready");
Console.WriteLine("====================================");

Console.WriteLine();
Console.WriteLine("Ask questions about your documents.");
Console.WriteLine("Type 'exit' anytime to quit.");
Console.WriteLine();

while (true)
{
    Console.Write("Ask: ");

    var question = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(question))
    {
        continue;
    }

    if (question.Equals(
        "exit",
        StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    await semanticSearchService.SearchAsync(question);
}

Console.WriteLine();
Console.WriteLine("====================================");
Console.WriteLine("       Product RAG Closed");
Console.WriteLine("====================================");
