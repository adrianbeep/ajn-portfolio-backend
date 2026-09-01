using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Persistence;
using System.Net.Http;

namespace Api.Tests;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly string _dbPath = $"test-{Guid.NewGuid():N}.db";

    public ApiWebApplicationFactory()
    {
        // Set environment variables BEFORE the factory's config is built.
        // CreateSlimBuilder reads them at startup (before ConfigureWebHost).
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $"DataSource={_dbPath}");
        Environment.SetEnvironmentVariable("Jwt__Secret", "super-secret-key-that-is-at-least-32-characters-long-for-hmacsha256!");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");
        Environment.SetEnvironmentVariable("Bootstrap__Token", "bootstrap-token-123");
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins", "http://localhost:3000");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext
            var descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Add test DbContext with in-memory database
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"DataSource={_dbPath}"));

            // Build and ensure DB is created with proper schema
            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public new async ValueTask DisposeAsync()
    {
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
        await base.DisposeAsync();
    }
}
