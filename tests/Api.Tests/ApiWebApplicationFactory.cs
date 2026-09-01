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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment variables for testing
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $"DataSource={_dbPath}");
        Environment.SetEnvironmentVariable("Jwt__Secret", "super-secret-key-that-is-at-least-32-characters-long-for-hmacsha256!");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");
        Environment.SetEnvironmentVariable("Bootstrap__Token", "bootstrap-token-123");
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins", "http://localhost:3000");
        
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext
            var descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Add test DbContext
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"DataSource={_dbPath}"));

            // Build and ensure DB is created
            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();

            // Seed bootstrap role
            db.Database.ExecuteSqlRaw("INSERT OR IGNORE INTO AspNetRoles (Id, Name, NormalizedName) VALUES ('00000000-0000-0000-0000-000000000001', 'Admin', 'ADMIN')");
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

public static class HttpClientExtensions
{
    public static string? GetCookie(this HttpClient client, string name)
    {
        if (client.DefaultRequestHeaders.TryGetValues("Cookie", out var cookies))
        {
            var cookie = cookies.FirstOrDefault(c => c.StartsWith($"{name}="));
            return cookie?.Substring(name.Length + 1);
        }
        // Check the cookie container via reflection
        var handlerField = client.GetType().GetField("_handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (handlerField == null) return null;
        
        var handler = handlerField.GetValue(client) as HttpClientHandler;
        if (handler?.CookieContainer == null) return null;
        
        var uri = client.BaseAddress;
        if (uri == null) return null;
        
        var foundCookie = handler.CookieContainer.GetCookies(uri).FirstOrDefault(c => c.Name == name);
        return foundCookie?.Value;
    }
}