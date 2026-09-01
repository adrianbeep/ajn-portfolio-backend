using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

[TestFixture]
public class AdminProjectCrudTests
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
        var email = $"admintest{Guid.NewGuid()}@test.com";
        var request = new { Email = email, Password = "Password123!" };
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/admin/auth/register");
        httpRequest.Content = JsonContent.Create(request);
        httpRequest.Headers.Add("X-Bootstrap-Token", "bootstrap-token-123");

        var response = await _client.SendAsync(httpRequest);
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            if (json.TryGetProperty("token", out var tokenProp))
            {
                _authToken = tokenProp.GetString() ?? "";
            }
        }

        // Set token as cookie so JWT middleware can read it
        if (!string.IsNullOrEmpty(_authToken))
        {
            _client.DefaultRequestHeaders.Add("Cookie", $"auth_token={_authToken}");
        }
    }

    [Test]
    public async Task CreateProject_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/admin/projects", new { Title = "Test", Slug = "test", Description = "Desc", UrlThumbnail = "https://img.com/1.jpg", Content = "Content" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateProject_WithValidData_Returns201()
    {
        await RegisterAndGetToken();
        var slug = $"test-project-{Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync("/admin/projects", new {
            Title = "Test Project",
            Slug = slug,
            Description = "A test project",
            UrlThumbnail = "https://img.com/test.jpg",
            Content = "# Test Content"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Test]
    public async Task CreateProject_WithEmptyTitle_Returns400()
    {
        await RegisterAndGetToken();
        var response = await _client.PostAsJsonAsync("/admin/projects", new {
            Title = "",
            Slug = "testslug",
            Description = "Desc",
            UrlThumbnail = "https://img.com/1.jpg",
            Content = "Content"
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateProject_WithInvalidSlug_Returns400()
    {
        await RegisterAndGetToken();
        var response = await _client.PostAsJsonAsync("/admin/projects", new {
            Title = "Test",
            Slug = "Invalid SLUG!",
            Description = "Desc",
            UrlThumbnail = "https://img.com/1.jpg",
            Content = "Content"
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task DeleteProject_WithoutAuth_Returns401()
    {
        var response = await _client.DeleteAsync("/admin/projects/some-slug");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteProject_Inexistent_Returns404()
    {
        await RegisterAndGetToken();
        var response = await _client.DeleteAsync("/admin/projects/this-does-not-exist");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteProject_SoftDeletes_Returns200()
    {
        await RegisterAndGetToken();
        var slug = $"delete-test-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/admin/projects", new {
            Title = "To Delete",
            Slug = slug,
            Description = "Desc",
            UrlThumbnail = "https://img.com/1.jpg",
            Content = "Content"
        });

        var response = await _client.DeleteAsync($"/admin/projects/{slug}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task RestoreProject_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsync("/admin/projects/some-slug/restore", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public class AuthResponse
{
    public string Token { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}