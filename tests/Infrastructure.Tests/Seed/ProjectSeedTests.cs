using FluentAssertions;
using Infrastructure.Persistence.Seed;
using Xunit;

namespace Infrastructure.Tests.Seed;

public class ProjectSeedTests
{
    // Esta es la guarda rápida de CI. El contenido sembrado se compara byte a byte
    // contra el snapshot de EF: si vuelve a colarse un \r, el arranque en Linux
    // aborta con PendingModelChangesWarning. Con este test el job "Build y tests"
    // lo detecta sin depender del smoke test del contenedor.
    [Fact]
    public void Data_ContentIsNormalizedAndNotEmpty()
    {
        ProjectSeed.Data.Should().NotBeEmpty();

        foreach (var project in ProjectSeed.Data)
        {
            project.Content.Should().NotContain("\r", "el contenido de '{0}' no debe llevar retornos de carro", project.Slug);
            project.Content.Should().Contain("\n", "el contenido de '{0}' debe conservar los saltos de linea en LF", project.Slug);
        }
    }

    // Guarda de las tecnologias sembradas: el limite del dominio es de 1 a 4
    // etiquetas por proyecto y ningun proyecto sale sin ellas.
    [Fact]
    public void Data_SeededProjectsCarryBetweenOneAndFourTechnologies()
    {
        ProjectSeed.Data.Should().NotBeEmpty();

        foreach (var project in ProjectSeed.Data)
        {
            project.Technologies.Should().NotBeNull();
            project.Technologies.Count.Should().BeInRange(
                1,
                4,
                "las tecnologias de '{0}' deben estar dentro del limite del dominio",
                project.Slug);
        }
    }

    // Los valores exactos que el contrato y el frontend dan por buenos.
    [Fact]
    public void Data_SeededTechnologiesAreTheExpectedOnes()
    {
        var portfolio = ProjectSeed.Data.Single(p => p.Slug == "mi-portfolio");
        portfolio.Technologies.Should().Equal("Astro", "TypeScript", "Vercel");

        var api = ProjectSeed.Data.Single(p => p.Slug == "api-gestion");
        api.Technologies.Should().Equal("ASP.NET Core", "EF Core", "SQLite");
    }

    // El contenido sembrado es la otra mitad de la fuente de verdad del
    // contrato: documenta el listado con las mismas tecnologias que se
    // guardan. Si una cambia y la otra no, esta guarda falla.
    [Fact]
    public void Data_SeededTechnologiesStayInSyncWithTheContractCopy()
    {
        var api = ProjectSeed.Data.Single(p => p.Slug == "api-gestion");

        api.Content.Should().Contain("\"technologies\": [\"Astro\", \"TypeScript\", \"Vercel\"],");
    }
}
