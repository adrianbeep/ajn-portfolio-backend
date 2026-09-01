using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

[TestFixture]
public class AuthTests
{
    private ApiWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

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

    [Test]
    public async Task Register_WithValidBootstrapToken_Returns200()
    {
        var request = new { Email = $"reg{Guid.NewGuid()}@test.com", Password = "Password123!" };
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/admin/auth/register");
        httpRequest.Content = JsonContent.Create(request);
        httpRequest.Headers.Add("X-Bootstrap-Token", "bootstrap-token-123");
        var response = await _client.SendAsync(httpRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Register_WithoutBootstrapToken_Returns401()
    {
        var request = new { Email = $"admin2{Guid.NewGuid()}@test.com", Password = "Password123!" };
        var response = await _client.PostAsJsonAsync("/admin/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Register_WithInvalidBootstrapToken_Returns401()
    {
        var request = new { Email = $"admin3{Guid.NewGuid()}@test.com", Password = "Password123!" };
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/admin/auth/register");
        httpRequest.Content = JsonContent.Create(request);
        httpRequest.Headers.Add("X-Bootstrap-Token", "wrong-token");
        var response = await _client.SendAsync(httpRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Login_WithValidCredentials_Returns200()
    {
        var email = $"login{Guid.NewGuid()}@test.com";
        var password = "Password123!";
        // Register first with bootstrap token
        using var regRequest = new HttpRequestMessage(HttpMethod.Post, "/admin/auth/register");
        regRequest.Content = JsonContent.Create(new { Email = email, Password = password });
        regRequest.Headers.Add("X-Bootstrap-Token", "bootstrap-token-123");
        await _client.SendAsync(regRequest);

        // Then login
        var response = await _client.PostAsJsonAsync("/admin/auth/login", new { Email = email, Password = password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Login_WithInvalidPassword_Returns401()
    {
        var email = $"loginfail{Guid.NewGuid()}@test.com";
        using var regRequest = new HttpRequestMessage(HttpMethod.Post, "/admin/auth/register");
        regRequest.Content = JsonContent.Create(new { Email = email, Password = "Password123!" });
        regRequest.Headers.Add("X-Bootstrap-Token", "bootstrap-token-123");
        await _client.SendAsync(regRequest);
        var response = await _client.PostAsJsonAsync("/admin/auth/login", new { Email = email, Password = "WrongPassword!" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Login_WithNonexistentEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/admin/auth/login", new { Email = "nobody@test.com", Password = "Password123!" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Logout_ReturnsOk()
    {
        var response = await _client.PostAsync("/admin/auth/logout", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}