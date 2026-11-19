using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Domain.ValueObjects;

public class TechnologySetTests
{
    [Fact]
    public void Create_WithNull_ReturnsEmptySet()
    {
        var result = TechnologySet.Create(null);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyCollection_ReturnsEmptySet()
    {
        var result = TechnologySet.Create(Array.Empty<string>());

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithFourItems_ReturnsFourItems()
    {
        var result = TechnologySet.Create(new[] { "Astro", "TypeScript", "EF Core", "SQLite" });

        result.Items.Should().HaveCount(4);
    }

    [Fact]
    public void Create_WithFiveItems_Throws()
    {
        Action act = () => TechnologySet.Create(new[] { "Astro", "TypeScript", "EF Core", "SQLite", "Vercel" });

        act.Should().Throw<ArgumentException>()
            .WithParameterName("technologies");
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_TrimsTheValue()
    {
        var result = TechnologySet.Create(new[] { "  React  " });

        result.Items.Should().ContainSingle().Which.Should().Be("React");
    }

    [Fact]
    public void Create_WithInternalRepeatedWhitespace_CollapsesToSingleSpaces()
    {
        var result = TechnologySet.Create(new[] { "ASP.NET   Core \t  10" });

        result.Items.Should().ContainSingle().Which.Should().Be("ASP.NET Core 10");
    }

    [Fact]
    public void Create_WithWhitespaceOnlyItem_Throws()
    {
        Action act = () => TechnologySet.Create(new[] { "   " });

        act.Should().Throw<ArgumentException>()
            .WithParameterName("technologies");
    }

    [Fact]
    public void Create_WithItemLongerThanMaxLength_Throws()
    {
        var tooLong = new string('a', TechnologySet.MaxLength + 1);

        Action act = () => TechnologySet.Create(new[] { tooLong });

        act.Should().Throw<ArgumentException>()
            .WithParameterName("technologies");
    }

    [Fact]
    public void Create_WithItemOfMaxLength_IsValid()
    {
        var atLimit = new string('a', TechnologySet.MaxLength);

        var result = TechnologySet.Create(new[] { atLimit });

        result.Items.Should().ContainSingle().Which.Should().Be(atLimit);
    }

    [Fact]
    public void Create_WithDuplicateIgnoringCase_Throws()
    {
        Action act = () => TechnologySet.Create(new[] { "React", "react" });

        act.Should().Throw<ArgumentException>()
            .WithParameterName("technologies");
    }

    [Fact]
    public void Create_WithDuplicateThatOnlyDiffersInWhitespace_Throws()
    {
        Action act = () => TechnologySet.Create(new[] { "React", "  React  " });

        act.Should().Throw<ArgumentException>()
            .WithParameterName("technologies");
    }

    [Fact]
    public void Create_PreservesTheOrderOfTheAuthor()
    {
        var result = TechnologySet.Create(new[] { "SQLite", "Astro", "EF Core" });

        result.Items.Should().Equal("SQLite", "Astro", "EF Core");
    }

    [Fact]
    public void Create_KeepsTheCasingOfTheAuthor()
    {
        var result = TechnologySet.Create(new[] { "react", "Next.js" });

        result.Items.Should().Equal("react", "Next.js");
    }

    [Fact]
    public void Create_DoesNotExposeTheSourceCollection()
    {
        var source = new List<string> { "React" };

        var result = TechnologySet.Create(source);
        source.Add("Next.js");

        result.Items.Should().Equal("React");
    }
}
