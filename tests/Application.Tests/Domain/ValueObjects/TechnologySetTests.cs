using Application.Projects.Commands.CreateProject;
using Application.Projects.Commands.UpdateProject;
using Domain.Exceptions;
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

        var exception = act.Should().Throw<DomainValidationException>().Which;

        exception.Field.Should().Be("Technologies");
        exception.Message.Should().Be(TechnologySet.Messages.TooManyItems);
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

        var exception = act.Should().Throw<DomainValidationException>().Which;

        exception.Field.Should().Be("Technologies");
        exception.Message.Should().Be(TechnologySet.Messages.EmptyValue);
    }

    [Fact]
    public void Create_WithItemLongerThanMaxLength_Throws()
    {
        var tooLong = new string('a', TechnologySet.MaxLength + 1);

        Action act = () => TechnologySet.Create(new[] { tooLong });

        var exception = act.Should().Throw<DomainValidationException>().Which;

        exception.Field.Should().Be("Technologies");
        exception.Message.Should().Be(TechnologySet.Messages.TooLong);
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

        var exception = act.Should().Throw<DomainValidationException>().Which;

        exception.Field.Should().Be("Technologies");
        exception.Message.Should().Be(TechnologySet.Messages.Duplicated);
    }

    [Fact]
    public void Create_WithDuplicateThatOnlyDiffersInWhitespace_Throws()
    {
        Action act = () => TechnologySet.Create(new[] { "React", "  React  " });

        var exception = act.Should().Throw<DomainValidationException>().Which;

        exception.Field.Should().Be("Technologies");
        exception.Message.Should().Be(TechnologySet.Messages.Duplicated);
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

    // El dominio es el dueno del texto de sus reglas: el validador de la capa
    // de aplicacion usa los mismos mensajes, asi el camino normal y el camino
    // de excepcion producen exactamente el mismo texto para el cliente.
    [Fact]
    public void Validators_UseTheDomainMessagesForTheTechnologyRules()
    {
        var tooMany = new[] { "Astro", "TypeScript", "EF Core", "SQLite", "Vercel" };
        var empty = new[] { "   " };
        var tooLong = new[] { new string('a', TechnologySet.MaxLength + 1) };
        var duplicated = new[] { "React", "react" };

        CreateValidatorMessage(tooMany).Should().Be(TechnologySet.Messages.TooManyItems);
        CreateValidatorMessage(empty).Should().Be(TechnologySet.Messages.EmptyValue);
        CreateValidatorMessage(tooLong).Should().Be(TechnologySet.Messages.TooLong);
        CreateValidatorMessage(duplicated).Should().Be(TechnologySet.Messages.Duplicated);

        UpdateValidatorMessage(tooMany).Should().Be(TechnologySet.Messages.TooManyItems);
        UpdateValidatorMessage(empty).Should().Be(TechnologySet.Messages.EmptyValue);
        UpdateValidatorMessage(tooLong).Should().Be(TechnologySet.Messages.TooLong);
        UpdateValidatorMessage(duplicated).Should().Be(TechnologySet.Messages.Duplicated);
    }

    // Paridad: el validador y el value object tienen que rechazar exactamente
    // las mismas entradas. Si divergen, la API responde 500 en vez de 400.
    public static TheoryData<IReadOnlyList<string>?> TechnologyInputs => new()
    {
        null!,
        Array.Empty<string>(),
        new[] { "Astro" },
        new[] { "Astro", "TypeScript", "EF Core", "SQLite" },
        new[] { "Astro", "TypeScript", "EF Core", "SQLite", "Vercel" },
        new[] { "" },
        new[] { "   " },
        new[] { "\t" },
        new[] { "Astro", "" },
        new[] { new string('a', TechnologySet.MaxLength) },
        new[] { new string('a', TechnologySet.MaxLength + 1) },
        new[] { new string('a', TechnologySet.MaxLength) + " " },
        new[] { new string('a', TechnologySet.MaxLength - 1) + "  " },
        new[] { new string('a', TechnologySet.MaxLength + 1) + "   " },
        new[] { "React", "react" },
        new[] { "React", "react " },
        new[] { "React", "  React  " },
        new[] { "React", "React Native" },
        new[] { "ASP.NET   Core" },
        new[] { "React", null! },
    };

    [Theory]
    [MemberData(nameof(TechnologyInputs))]
    public void ValidatorsAndValueObject_AgreeOnTheSameVerdict(IReadOnlyList<string>? technologies)
    {
        var valueObjectAccepts = Accepts(technologies);

        CreateValidatorAccepts(technologies).Should().Be(
            valueObjectAccepts,
            "el validador de creacion y el value object tienen que coincidir en cada entrada");

        UpdateValidatorAccepts(technologies).Should().Be(
            valueObjectAccepts,
            "el validador de actualizacion y el value object tienen que coincidir en cada entrada");
    }

    private static string CreateValidatorMessage(IReadOnlyList<string> technologies)
    {
        var command = new CreateProjectCommand(
            "Title", "my-slug", "https://img.png", "Desc", "Content", technologies);

        return new CreateProjectCommandValidator().Validate(command).Errors
            .Select(error => error.ErrorMessage)
            .First();
    }

    private static string UpdateValidatorMessage(IReadOnlyList<string> technologies)
    {
        var command = new UpdateProjectCommand(
            "my-slug", "Title", "Desc", "https://img.png", "Content", technologies);

        return new UpdateProjectCommandValidator().Validate(command).Errors
            .Select(error => error.ErrorMessage)
            .First();
    }

    private static bool CreateValidatorAccepts(IReadOnlyList<string>? technologies)
    {
        var command = new CreateProjectCommand(
            "Title", "my-slug", "https://img.png", "Desc", "Content", technologies);

        return new CreateProjectCommandValidator().Validate(command).IsValid;
    }

    private static bool UpdateValidatorAccepts(IReadOnlyList<string>? technologies)
    {
        var command = new UpdateProjectCommand(
            "my-slug", "Title", "Desc", "https://img.png", "Content", technologies);

        return new UpdateProjectCommandValidator().Validate(command).IsValid;
    }

    private static bool Accepts(IReadOnlyList<string>? technologies)
    {
        try
        {
            TechnologySet.Create(technologies);
            return true;
        }
        catch (DomainValidationException)
        {
            return false;
        }
    }
}
