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
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("TESTING_CONNECTION_STRING", $"DataSource={_dbPath}");
        Environment.SetEnvironmentVariable("ASPNETCORE_BOOTSTRAP__TOKEN", "bootstrap-token-123");
        Environment.SetEnvironmentVariable("ASPNETCORE_CORS__ALLOWEDORIGINS", "http://localhost:3000");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"DataSource={_dbPath}"));

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