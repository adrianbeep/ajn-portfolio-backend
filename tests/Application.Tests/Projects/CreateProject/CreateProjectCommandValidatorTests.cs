using Application.Projects.Commands.CreateProject;
using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Projects.CreateProject;

public class CreateProjectCommandValidatorTests
{
    private readonly CreateProjectCommandValidator _validator = new();

    [Fact]
    public void Validate_WithNullTechnologies_IsValid()
    {
        var result = _validator.Validate(Command(null));

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyTechnologies_IsValid()
    {
        var result = _validator.Validate(Command(Array.Empty<string>()));

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithFourTechnologies_IsValid()
    {
        var result = _validator.Validate(Command(new[] { "Astro", "TypeScript", "EF Core", "SQLite" }));

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithFiveTechnologies_IsInvalid()
    {
        var result = _validator.Validate(Command(new[] { "Astro", "TypeScript", "EF Core", "SQLite", "Vercel" }));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Technologies");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Validate_WithEmptyItem_IsInvalid(string empty)
    {
        var result = _validator.Validate(Command(new[] { "Astro", empty }));

        result.IsValid.Should().BeFalse();
        // El dominio reporta el elemento que incumple con su indice: "Technologies[1]".
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Technologies[1]"
            && e.ErrorMessage == "Technologies cannot contain empty values.");
    }

    [Fact]
    public void Validate_WithItemLongerThanMaxLength_IsInvalid()
    {
        var tooLong = new string('a', TechnologySet.MaxLength + 1);

        var result = _validator.Validate(Command(new[] { tooLong }));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Technologies[0]"
            && e.ErrorMessage == $"Technologies items must not exceed {TechnologySet.MaxLength} characters.");
    }

    [Fact]
    public void Validate_WithItemOfMaxLength_IsValid()
    {
        var atLimit = new string('a', TechnologySet.MaxLength);

        var result = _validator.Validate(Command(new[] { atLimit }));

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithItemPaddedToMaxLengthOnceNormalized_IsValid()
    {
        // 30 caracteres utiles mas un espacio: el valor normalizado mide 30 y
        // la regla se mide sobre el valor normalizado, no sobre el crudo.
        var padded = new string('a', TechnologySet.MaxLength) + " ";

        var result = _validator.Validate(Command(new[] { padded }));

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithDuplicatesIgnoringCase_IsInvalid()
    {
        var result = _validator.Validate(Command(new[] { "React", "react" }));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Technologies");
    }

    [Fact]
    public void Validate_WithDuplicatesThatOnlyDifferInWhitespace_IsInvalid()
    {
        var result = _validator.Validate(Command(new[] { "React", "  React  " }));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Technologies");
    }

    [Fact]
    public void Validate_WithValidTechnologies_ProducesNoErrors()
    {
        var result = _validator.Validate(Command(new[] { "Astro", "TypeScript" }));

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    private static CreateProjectCommand Command(IReadOnlyList<string>? technologies)
        => new("Title", "my-slug", "https://img.png", "Desc", "Content", technologies);
}
