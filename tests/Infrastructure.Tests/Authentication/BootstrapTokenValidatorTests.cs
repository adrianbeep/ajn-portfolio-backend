using FluentAssertions;
using Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Xunit;

namespace Infrastructure.Tests.Authentication;

public class BootstrapTokenValidatorTests
{
    private const string ExpectedToken = "mi-token-secreto-123";

    private BootstrapTokenValidator CreateValidator(string? token = null)
    {
        var settings = new BootstrapSettings
        {
            Token = token ?? ExpectedToken
        };
        return new BootstrapTokenValidator(Options.Create(settings));
    }

    [Fact]
    public async Task IsValid_CorrectToken_ReturnsTrue()
    {
        var validator = CreateValidator();

        var result = await validator.IsValid(ExpectedToken);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsValid_IncorrectToken_ReturnsFalse()
    {
        var validator = CreateValidator();

        var result = await validator.IsValid("token-incorrecto");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValid_NullToken_ReturnsFalse()
    {
        var validator = CreateValidator();

        var result = await validator.IsValid(null);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValid_EmptyToken_ReturnsFalse()
    {
        var validator = CreateValidator();

        var result = await validator.IsValid(string.Empty);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValid_WhitespaceOnlyToken_ReturnsFalse()
    {
        var validator = CreateValidator();

        var result = await validator.IsValid("   ");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValid_TokenWithDifferentCase_ReturnsFalse()
    {
        var validator = CreateValidator();

        var result = await validator.IsValid("MI-TOKEN-SECRETO-123");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValid_TokenWithTrailingSpace_ReturnsFalse()
    {
        var validator = CreateValidator();

        var result = await validator.IsValid(ExpectedToken + " ");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValid_CaseSensitive_ExactMatch()
    {
        var validator = CreateValidator("ExactCaseToken");

        var result = await validator.IsValid("exactcasetoken");

        result.Should().BeFalse();
    }
}
