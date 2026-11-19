using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ProjectRepository = Infrastructure.Persistence.Repositories.ProjectRepository;
using Xunit;

namespace Infrastructure.Tests.Persistence;

// Guarda contra el N+1 del listado de proyectos.
//
// El listado se resuelve con dos viajes a la base: un COUNT y un SELECT
// (GetProjectsQueryHandler llama a CountNotDeletedAsync y a
// GetPagedNotDeletedAsync). Agregar la columna de tecnologias no puede cambiar
// ese numero, y el SELECT tiene que traer la columna en el mismo viaje.
public class ProjectListingQueryCountTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ProjectRepository _repository;
    private readonly CommandCountingInterceptor _interceptor = new();
    private int _slugCounter;

    public ProjectListingQueryCountTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .AddInterceptors(_interceptor)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        // Clear seed data so each test starts with a clean slate
        _db.Projects.RemoveRange(_db.Projects);
        _db.SaveChanges();

        _repository = new ProjectRepository(_db);
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }

    [Fact]
    public async Task Listing_IssuesExactlyTwoCommands_RegardlessOfTheNumberOfProjects()
    {
        var ct = TestContext.Current.CancellationToken;

        await SeedProjectsAsync(3, ct);

        _interceptor.Reset();

        var total = await _repository.CountNotDeletedAsync(ct);
        var projects = await _repository.GetPagedNotDeletedAsync(1, 20, ct);

        total.Should().Be(3);
        projects.Should().HaveCount(3);
        _interceptor.Count.Should().Be(2);

        // Con muchas mas filas el conteo no se mueve: si el listado hiciera
        // una consulta por proyecto, el numero cresceria con el catalogo.
        await SeedProjectsAsync(12, ct);

        _interceptor.Reset();

        total = await _repository.CountNotDeletedAsync(ct);
        projects = await _repository.GetPagedNotDeletedAsync(1, 20, ct);

        total.Should().Be(15);
        projects.Should().HaveCount(15);
        _interceptor.Count.Should().Be(2);
    }

    [Fact]
    public async Task Listing_UsesOneCountAndOneSelect()
    {
        var ct = TestContext.Current.CancellationToken;

        await SeedProjectsAsync(4, ct);

        _interceptor.Reset();

        await _repository.CountNotDeletedAsync(ct);
        await _repository.GetPagedNotDeletedAsync(1, 20, ct);

        var commands = _interceptor.Commands;

        commands.Should().HaveCount(2);

        var counts = commands.Where(c => c.Contains("COUNT(", StringComparison.OrdinalIgnoreCase)).ToList();
        counts.Should().HaveCount(1);

        var selects = commands.Where(c => !c.Contains("COUNT(", StringComparison.OrdinalIgnoreCase)).ToList();
        selects.Should().HaveCount(1);
        selects[0].Should().Contain("projects");
    }

    [Fact]
    public async Task Listing_ReadsTechnologiesInTheSameRoundTrip()
    {
        var ct = TestContext.Current.CancellationToken;

        await SeedProjectsAsync(2, ct);

        _interceptor.Reset();

        await _repository.CountNotDeletedAsync(ct);
        var projects = await _repository.GetPagedNotDeletedAsync(1, 20, ct);

        // La columna de tecnologias se lee en el mismo SELECT del listado: no
        // hay una consulta extra por proyecto.
        var select = _interceptor.Commands
            .Single(c => !c.Contains("COUNT(", StringComparison.OrdinalIgnoreCase));

        select.Should().Contain("Technologies");

        projects.Should().OnlyContain(p => p.Technologies.Count == 2);
        projects.SelectMany(p => p.Technologies).Should().Contain("Astro");
        projects.SelectMany(p => p.Technologies).Should().Contain("EF Core");
    }

    private async Task SeedProjectsAsync(int count, CancellationToken ct)
    {
        for (var i = 0; i < count; i++)
        {
            var slug = $"proyecto-{_slugCounter++}";

            var project = new Project(
                $"Proyecto {slug}",
                slug,
                "https://example.com/thumb.png",
                "Descripcion",
                "# Contenido",
                new[] { "Astro", "EF Core" });

            await _repository.AddAsync(project, ct);
        }

        await _db.SaveChangesAsync(ct);
    }
}
