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
        _authToken = null;
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
        using var request = new HttpRequestMessage(HttpMethod.Post, "/admin/auth/register");
        request.Content = JsonContent.Create(new { Email = email, Password = "Password123!" });
        request.Headers.Add("X-Bootstrap-Token", "bootstrap-token-123");

        var response = await _client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (content != null)
            {
                _authToken = content.Token;
            }
        }
    }

    private async Task<HttpResponseMessage> AdminPostAsync(string path, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Content = JsonContent.Create(body);
        if (!string.IsNullOrEmpty(_authToken))
            request.Headers.Add("Cookie", $"auth_token={_authToken}");
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> AdminDeleteAsync(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, path);
        if (!string.IsNullOrEmpty(_authToken))
            request.Headers.Add("Cookie", $"auth_token={_authToken}");
        return await _client.SendAsync(request);
    }

    [Test]
    public async Task CreateContribution_WithoutAuth_Returns401()
    {
        var response = await AdminPostAsync("/admin/contributions", new { Title = "Test", Slug = "test", Description = "Desc", UrlThumbnail = "https://img.com/1.jpg", UrlPrContribution = "https://github.com/pr/1", Content = "Content" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateContribution_WithValidData_Returns201()
    {
        await RegisterAndGetToken();
        var slug = $"test-contrib-{Guid.NewGuid():N}";
        var response = await AdminPostAsync("/admin/contributions", new {
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
        var response = await AdminDeleteAsync("/admin/contributions/some-slug");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}