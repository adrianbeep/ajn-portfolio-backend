using FluentAssertions;
using Infrastructure.Persistence.Seed;
using Xunit;

namespace Infrastructure.Tests.Seed;

public class SeedTextTests
{
    // Guardarraíl de plataforma: EF compara el seed contra el snapshot byte a byte,
    // así que el mismo código tiene que producir el mismo valor en Windows (CRLF)
    // y en Linux (LF). Si el salto de línea no se normaliza, el arranque aborta.
    [Theory]
    [InlineData("a\r\nb", "a\nb")]
    [InlineData("a\rb", "a\nb")]
    [InlineData("a\nb", "a\nb")]
    public void Normalize_TranslatesEveryLineEndingToLf(string input, string expected)
    {
        SeedText.Normalize(input).Should().Be(expected);
    }
}
