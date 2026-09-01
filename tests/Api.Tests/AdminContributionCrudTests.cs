using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

[TestFixture]
public class AdminContributionCrudTests
{
    private ApiWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private string? _authToken;

    [SetUp]
    public void Setup()
    {
        _factory = new ApiWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task RegisterAndGetToken()
    {
        var email = $"contribtest{Guid.NewGuid()}@test.com";
        var response = await _client.PostAsJsonAsync("/admin/auth/register", new { Email = email, Password = "Password123!" });
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookieValues))
        {
            var cookie = setCookieValues.FirstOrDefault(c => c.StartsWith("auth_token="));
            if (cookie != null)
            {
                _authToken = cookie.Substring("auth_token=".Length).Split(';').First();
            }
        }
        if (string.IsNullOrEmpty(_authToken))
        {
            var content = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (content != null)
            {
                _authToken = content.Token;
            }
        }
        if (!string.IsNullOrEmpty(_authToken))
        {
            _client.DefaultRequestHeaders.Add("Cookie", $"auth_token={_authToken}");
        }
    }

    [Test]
    public async Task CreateContribution_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/admin/contributions", new { Title = "Test", Slug = "test", Description = "Desc", UrlThumbnail = "https://img.com/1.jpg", UrlPrContribution = "https://github.com/pr/1", Content = "Content" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateContribution_WithValidData_Returns201()
    {
        await RegisterAndGetToken();
        var slug = $"test-contrib-{Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync("/admin/contributions", new { 
            Title = "Test Contribution", 
            Slug = slug, 
            Description = "A test contribution", 
            UrlThumbnail = "https://img.com/test.jpg",
            UrlPrContribution = "https://github.com/test/pr/1",
            Content = "# Test Content" 
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Test]
    public async Task DeleteContribution_WithoutAuth_Returns401()
    {
        var response = await _client.DeleteAsync("/admin/contributions/some-slug");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}