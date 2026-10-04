using Application.Abstractions.Authentication;
using Application.Abstractions.Persistence;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Xunit;
using FluentAssertions;
using Infrastructure.Persistence;

namespace Infrastructure.Tests;

public class DependencyInjectionTests : IDisposable
{
    private readonly string? _previousDbPath;

    public DependencyInjectionTests()
    {
        // La DB ya no se lee desde appsettings. Se resuelve via Infrastructure.Persistence.DatabaseConnection,
        // que prioriza la env var PORTFOLIO_DB_PATH. Para este test apuntamos a una SQLite en memoria
        // para no dejar archivos en el repo y no chocar con otros tests.
        _previousDbPath = Environment.GetEnvironmentVariable(DatabaseConnection.EnvironmentVariable);
        Environment.SetEnvironmentVariable(DatabaseConnection.EnvironmentVariable, "Data Source=:memory:");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(DatabaseConnection.EnvironmentVariable, _previousDbPath);
    }

    [Fact]
    public void AddInfrastructure_ShouldResolveAllDependencies()
    {
        // Arrange: Simulamos una configuración en memoria (appsettings). Antes acá vivia el
        // ConnectionStrings:DefaultConnection, pero la DB ahora se resuelve por
        // DatabaseConnection.Resolve() (anchored al repo + env var override).
        var inMemorySettings = new Dictionary<string, string>();

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();

// MediatR 14.x requires ILoggerFactory
        services.AddLogging();

        // Llamamos a tus dos métodos de extensión nuevos
        services.AddApplication();
        services.AddInfrastructure(configuration);

        // BuildServiceProvider crea el contenedor real (el libro de recetas listo para usar)
        var provider = services.BuildServiceProvider();

        // Act & Assert: Intentamos pedirle cosas al contenedor.
        // Si no las puede armar, tira excepción y falla el test.

        using var scope = provider.CreateScope(); // Creamos un scope HTTP simulado
        var scopedProvider = scope.ServiceProvider;

        // 1. Probamos Application
        scopedProvider.GetRequiredService<ISender>().Should().NotBeNull(); // MediatR

        // 2. Probamos Base de datos y Repos
        scopedProvider.GetRequiredService<AppDbContext>().Should().NotBeNull();
        scopedProvider.GetRequiredService<IUnitOfWork>().Should().NotBeNull();
        scopedProvider.GetRequiredService<IProjectRepository>().Should().NotBeNull();

        // 3. Probamos Identity y Auth
        scopedProvider.GetRequiredService<UserManager<ApplicationUser>>().Should().NotBeNull();
        scopedProvider.GetRequiredService<IJwtTokenGenerator>().Should().NotBeNull();
    }
}