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

    private async Task<HttpResponseMessage> AdminPostAsync(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (!string.IsNullOrEmpty(_authToken))
            request.Headers.Add("Cookie", $"auth_token={_authToken}");
        return await _client.SendAsync(request);
    }

    [Test]
    public async Task CreateProject_WithoutAuth_Returns401()
    {
        var response = await AdminPostAsync("/admin/projects", new { Title = "Test", Slug = "test", Description = "Desc", UrlThumbnail = "https://img.com/1.jpg", Content = "Content" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Register_Login_CreateProject_Sequence()
    {
        // Step 1: Register
        var email = $"seq{Guid.NewGuid()}@test.com";
        using var regRequest = new HttpRequestMessage(HttpMethod.Post, "/admin/auth/register");
        regRequest.Content = JsonContent.Create(new { Email = email, Password = "Password123!" });
        regRequest.Headers.Add("X-Bootstrap-Token", "bootstrap-token-123");
        var regResponse = await _client.SendAsync(regRequest);
        regResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Login to get fresh token
        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/admin/auth/login");
        loginRequest.Content = JsonContent.Create(new { Email = email, Password = "Password123!" });
        var loginResponse = await _client.SendAsync(loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginJson = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var token = loginJson!.Token;

        // Step 3: Create project with token in Authorization header
        var slug = $"seq-{Guid.NewGuid():N}";
        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/admin/projects");
        createRequest.Content = JsonContent.Create(new {
            Title = "Seq Project",
            Slug = slug,
            Description = "Desc",
            UrlThumbnail = "https://img.com/1.jpg",
            Content = "Content"
        });
        createRequest.Headers.Add("Authorization", $"Bearer {token}");

        var createResponse = await _client.SendAsync(createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Test]
    public async Task CreateProject_WithValidData_Returns201()
    {
        await RegisterAndGetToken();
        var slug = $"test-project-{Guid.NewGuid():N}";
        var response = await AdminPostAsync("/admin/projects", new {
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
        var response = await AdminPostAsync("/admin/projects", new {
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
        var response = await AdminPostAsync("/admin/projects", new {
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
        var response = await AdminDeleteAsync("/admin/projects/some-slug");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteProject_Inexistent_Returns404()
    {
        await RegisterAndGetToken();
        var response = await AdminDeleteAsync("/admin/projects/this-does-not-exist");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteProject_SoftDeletes_Returns200()
    {
        await RegisterAndGetToken();
        var slug = $"delete-test-{Guid.NewGuid():N}";
        await AdminPostAsync("/admin/projects", new {
            Title = "To Delete",
            Slug = slug,
            Description = "Desc",
            UrlThumbnail = "https://img.com/1.jpg",
            Content = "Content"
        });

        var response = await AdminDeleteAsync($"/admin/projects/{slug}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task RestoreProject_WithoutAuth_Returns401()
    {
        var response = await AdminPostAsync("/admin/projects/some-slug/restore");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public class AuthResponse
{
    public string Token { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}