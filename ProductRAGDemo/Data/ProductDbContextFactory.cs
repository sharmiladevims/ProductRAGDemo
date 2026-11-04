using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pgvector.EntityFrameworkCore;

namespace ProductRAGDemo.Data;

public sealed class ProductDbContextFactory
    : IDesignTimeDbContextFactory<ProductDbContext>
{
    public ProductDbContext CreateDbContext(
        string[] args)
    {
        var configuration =
            new ConfigurationBuilder()
                .AddJsonFile(
                    "appsettings.json",
                    optional: false)
                .Build();

        var connectionString =
            configuration[
                "DatabaseSettings:DefaultConnection"];

        var optionsBuilder =
            new DbContextOptionsBuilder<ProductDbContext>();

        optionsBuilder.UseNpgsql(
            connectionString,
            x => x.UseVector());

        return new ProductDbContext(
            optionsBuilder.Options);
    }
}