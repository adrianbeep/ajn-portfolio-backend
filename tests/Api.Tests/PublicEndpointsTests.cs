using FluentAssertions;
using NUnit.Framework;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

[TestFixture]
public class PublicEndpointsTests
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
    public async Task GetProjects_ReturnsOk()
    {
        var response = await _client.GetAsync("/projects");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetProjects_ReturnsPaginatedResponse()
    {
        var response = await _client.GetAsync("/projects");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"items\"");
        content.Should().Contain("\"total\"");
    }

    [Test]
    public async Task GetProjectBySlug_Inexistent_Returns404()
    {
        var response = await _client.GetAsync("/projects/this-does-not-exist");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetContributions_ReturnsOk()
    {
        var response = await _client.GetAsync("/contributions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetContributionBySlug_Inexistent_Returns404()
    {
        var response = await _client.GetAsync("/contributions/this-does-not-exist");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}