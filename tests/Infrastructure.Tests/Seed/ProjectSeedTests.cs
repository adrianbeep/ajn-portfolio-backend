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
}
