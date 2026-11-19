using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Tests;

// Pruebas de integracion del campo `technologies` a traves de la API real:
// lo que ve el frontend es lo que se comprueba aqui.
[TestFixture]
public class ProjectTechnologiesTests
{
    private const string BootstrapToken = "bootstrap-token-123";
    private const string Password = "Password123!";

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
    public async Task GetProjects_ReturnsTechnologiesForEveryItemInAuthorOrder()
    {
        var response = await _client.GetAsync("/projects");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = ReadItems(await response.Content.ReadAsStringAsync());

        items.Should().HaveCountGreaterThanOrEqualTo(2);

        // Toda entrada del listado expone el arreglo, nunca falta la clave.
        foreach (var item in items)
        {
            item.TryGetProperty("technologies", out var technologies).Should().BeTrue();
            technologies.ValueKind.Should().Be(JsonValueKind.Array);
        }

        var portfolio = items.Single(i => i.GetProperty("slug").GetString() == "mi-portfolio");
        ReadTechnologies(portfolio).Should().Equal("Astro", "TypeScript", "Vercel");

        var api = items.Single(i => i.GetProperty("slug").GetString() == "api-gestion");
        ReadTechnologies(api).Should().Equal("ASP.NET Core", "EF Core", "SQLite");
    }

    [Test]
    public async Task GetProjectBySlug_ReturnsTheSameTechnologiesAsTheListing()
    {
        var response = await _client.GetAsync("/projects/mi-portfolio");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        ReadTechnologies(document.RootElement).Should().Equal("Astro", "TypeScript", "Vercel");
    }

    [Test]
    public async Task PostProject_WithMoreThanFourTechnologies_Returns400()
    {
        var token = await AuthenticateAsync();
        var payload = NewProjectPayload(NewSlug(), new[] { "Astro", "TypeScript", "Vercel", "EF Core", "SQLite" });

        var response = await PostAdminProjectAsync(payload, token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Technologies");

        // La respuesta es un ProblemDetails, no un error de servidor.
        using var problem = JsonDocument.Parse(body);
        problem.RootElement.GetProperty("status").GetInt32().Should().Be(400);
        problem.RootElement.GetProperty("title").GetString().Should().Be("ValidationFailed");
    }

    [Test]
    public async Task PostProject_WithDuplicateTechnologies_Returns400()
    {
        var token = await AuthenticateAsync();
        var payload = NewProjectPayload(NewSlug(), new[] { "React", "react" });

        var response = await PostAdminProjectAsync(payload, token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Technologies");
    }

    [Test]
    public async Task PostProject_WithEmptyTechnology_Returns400()
    {
        var token = await AuthenticateAsync();
        var payload = NewProjectPayload(NewSlug(), new[] { "  " });

        var response = await PostAdminProjectAsync(payload, token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Technologies");
    }

    [Test]
    public async Task PostProject_WithTechnologyLongerThanThirtyCharacters_Returns400()
    {
        var token = await AuthenticateAsync();
        var payload = NewProjectPayload(NewSlug(), new[] { new string('a', 31) });

        var response = await PostAdminProjectAsync(payload, token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Technologies");
    }

    [Test]
    public async Task PostProject_WithEmptyTechnologiesArray_Returns201AndStoresAnEmptyList()
    {
        var token = await AuthenticateAsync();
        var slug = NewSlug();
        var payload = NewProjectPayload(slug, Array.Empty<string>());

        var response = await PostAdminProjectAsync(payload, token);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var detail = await _client.GetAsync($"/projects/{slug}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await detail.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        ReadTechnologies(document.RootElement).Should().BeEmpty();
    }

    [Test]
    public async Task PostProject_WithTwoTechnologies_Returns201AndKeepsTheAuthorOrder()
    {
        var token = await AuthenticateAsync();
        var slug = NewSlug();
        var payload = NewProjectPayload(slug, new[] { "TypeScript", "Astro" });

        var response = await PostAdminProjectAsync(payload, token);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var detail = await _client.GetAsync($"/projects/{slug}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await detail.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        ReadTechnologies(document.RootElement).Should().Equal("TypeScript", "Astro");
    }

    [Test]
    public async Task PutProject_WithNewTechnologies_ReplacesTheStoredList()
    {
        var token = await AuthenticateAsync();
        var slug = NewSlug();

        var created = await PostAdminProjectAsync(NewProjectPayload(slug, new[] { "Astro", "Vercel" }), token);
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = NewProjectPayload(slug, new[] { "Blazor", ".NET" });

        var response = await PutAdminProjectAsync(slug, payload, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await _client.GetAsync($"/projects/{slug}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await detail.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        ReadTechnologies(document.RootElement).Should().Equal("Blazor", ".NET");
    }

    [Test]
    public async Task PutProject_WithoutTechnologies_ClearsTheStoredList()
    {
        var token = await AuthenticateAsync();
        var slug = NewSlug();

        var created = await PostAdminProjectAsync(NewProjectPayload(slug, new[] { "Astro", "Vercel" }), token);
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        // El PUT reemplaza la representacion completa: si el campo no viaja,
        // las tecnologias guardadas se vacian.
        var payload = NewProjectPayload(slug, null);

        var response = await PutAdminProjectAsync(slug, payload, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await _client.GetAsync($"/projects/{slug}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await detail.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        ReadTechnologies(document.RootElement).Should().BeEmpty();
    }

    [Test]
    public async Task GetProjectBySlug_WithoutTechnologies_SerializesAnEmptyArrayAndNeverNull()
    {
        var token = await AuthenticateAsync();
        var slug = NewSlug();

        var created = await PostAdminProjectAsync(NewProjectPayload(slug, null), token);
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await _client.GetStringAsync($"/projects/{slug}");

        body.Should().Contain("\"technologies\":[]");
        body.Should().NotContain("\"technologies\":null");
    }

    // Registra un admin con el token de bootstrap, inicia sesion y devuelve el
    // JWT para mandarlo como "Authorization: Bearer ...". El registro ya le
    // asigna el rol Admin, asi que ese token alcanza para los endpoints
    // administrativos.
    private async Task<string> AuthenticateAsync()
    {
        var email = $"admin{Guid.NewGuid()}@test.com";

        using var registerRequest = new HttpRequestMessage(HttpMethod.Post, "/admin/auth/register");
        registerRequest.Content = JsonContent.Create(new { Email = email, Password = Password });
        registerRequest.Headers.Add("X-Bootstrap-Token", BootstrapToken);

        var registerResponse = await _client.SendAsync(registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await _client.PostAsJsonAsync(
            "/admin/auth/login",
            new { Email = email, Password = Password });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(loginBody);

        return document.RootElement.GetProperty("token").GetString()!;
    }

    private async Task<HttpResponseMessage> PostAdminProjectAsync(
        Dictionary<string, object?> payload,
        string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/admin/projects")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PutAdminProjectAsync(
        string slug,
        Dictionary<string, object?> payload,
        string token)
    {
        // El id viaja en el cuerpo pero el servidor lo ignora: la identidad del
        // proyecto es el slug de la ruta.
        var body = new Dictionary<string, object?>(payload) { ["id"] = Guid.NewGuid() };

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/admin/projects/{slug}")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _client.SendAsync(request);
    }

    // El cuerpo de una peticion de proyecto. Si `technologies` es null, el
    // campo no viaja en el JSON: esa es la forma de probar el PUT que borra
    // las tecnologias guardadas.
    private static Dictionary<string, object?> NewProjectPayload(
        string slug,
        string[]? technologies = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["title"] = "Proyecto con tecnologias",
            ["slug"] = slug,
            ["urlThumbnail"] = "https://example.com/thumb.png",
            ["description"] = "Descripcion del proyecto",
            ["content"] = "# Contenido"
        };

        if (technologies is not null)
            payload["technologies"] = technologies;

        return payload;
    }

    private static string NewSlug() => $"tech-{Guid.NewGuid():N}";

    private static List<JsonElement> ReadItems(string body)
    {
        using var document = JsonDocument.Parse(body);

        // Los elementos se clonan porque un JsonElement deja de ser valido
        // cuando el documento del que salio se descarta.
        return document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.Clone())
            .ToList();
    }

    private static string[] ReadTechnologies(JsonElement project)
    {
        return project.GetProperty("technologies")
            .EnumerateArray()
            .Select(value => value.GetString() ?? string.Empty)
            .ToArray();
    }
}
