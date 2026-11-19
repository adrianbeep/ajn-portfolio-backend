using Application.Projects.Commands.CreateProject;
using Api.Common;
using Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Tests;

// Red de seguridad del dominio: si el validador de la capa de aplicacion y
// las reglas del dominio se desalinean, la excepcion de dominio tiene que
// volver al cliente como un 400 con ProblemDetails y no como un 500 (ni como
// un 500 sin cuerpo, que es lo que pasaba antes de este arreglo).
[TestFixture]
public class DomainValidationTests
{
    private const string BootstrapToken = "bootstrap-token-123";
    private const string Password = "Password123!";

    private static readonly string[] TooManyTechnologies =
        ["Astro", "TypeScript", "EF Core", "SQLite", "Vercel"];

    private DriftedValidatorFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new DriftedValidatorFactory();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public async Task TearDown()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Test]
    public void TheDriftedFactoryReallyReplacesTheRealValidator()
    {
        // Si el validador real siguiera registrado, la peticion la rechazaria
        // el validador y no el dominio: los tests de abajo no probarian nada.
        var validators = _factory.Services.GetServices<IValidator<CreateProjectCommand>>().ToList();

        validators.Should().ContainSingle();
        validators.Single().Should().BeOfType<PermissiveCreateProjectValidator>();
    }

    [Test]
    public async Task PostProject_WithTooManyTechnologies_Returns400WithTheDomainMessage()
    {
        var token = await AuthenticateAsync(_client);

        var response = await PostProjectAsync(_client, token, TooManyTechnologies);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var body = await response.Content.ReadAsStringAsync();
        using var problem = JsonDocument.Parse(body);

        problem.RootElement.GetProperty("title").GetString().Should().Be("ValidationFailed");
        problem.RootElement.GetProperty("detail").GetString().Should().Be(
            "Technologies : Technologies must not contain more than 4 items.");
    }

    [Test]
    public async Task PostProject_WithTooManyTechnologiesAndAcceptHtml_Returns400WithABody()
    {
        // Antes de la red de seguridad este caso devolvia un 500 pelado: sin
        // cuerpo y sin tipo de contenido, porque el ProblemDetailsService
        // respeta el header Accept y con "text/html" no escribe nada.
        var token = await AuthenticateAsync(_client);

        var response = await PostProjectAsync(_client, token, TooManyTechnologies, "text/html");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var body = await response.Content.ReadAsStringAsync();

        body.Should().NotBeNullOrWhiteSpace();

        using var problem = JsonDocument.Parse(body);
        problem.RootElement.GetProperty("title").GetString().Should().Be("ValidationFailed");
    }

    [Test]
    public async Task TheDomainSafetyNetAndTheValidatorSayTheSameThing()
    {
        // Camino normal: el validador real rechaza antes de llegar al dominio.
        await using var normalFactory = new ApiWebApplicationFactory();
        using var normalClient = normalFactory.CreateClient();
        var normalToken = await AuthenticateAsync(normalClient);

        var normal = await PostProjectAsync(normalClient, normalToken, TooManyTechnologies);

        normal.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Camino de excepcion: el validador deja pasar el dato y lo rechaza el
        // dominio. Los dos caminos tienen que decir exactamente lo mismo.
        var driftedToken = await AuthenticateAsync(_client);
        var drifted = await PostProjectAsync(_client, driftedToken, TooManyTechnologies);

        drifted.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // No solo el detail: el cliente no puede distinguir el camino que
        // produjo el error, asi que tampoco puede distinguir el encabezado.
        drifted.Content.Headers.ContentType!.ToString()
            .Should().Be(normal.Content.Headers.ContentType!.ToString());

        var normalProperties = await ReadPropertyNamesAsync(normal);
        var driftedProperties = await ReadPropertyNamesAsync(drifted);

        driftedProperties.Should().BeEquivalentTo(normalProperties);

        (await ReadDetailAsync(drifted)).Should().Be(await ReadDetailAsync(normal));
    }

    [Test]
    public async Task TryHandleAsync_WithADomainViolation_WritesA400()
    {
        var handler = new DomainExceptionHandler();
        var context = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(
            context,
            new DomainValidationException(
                "Technologies",
                "Technologies must not contain more than 4 items."),
            CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.ContentType.Should().Be("application/problem+json");
    }

    [Test]
    public async Task TryHandleAsync_WithANonDomainException_DeclinesIt()
    {
        // Un fallo de programacion no es un 400: tiene que seguir siendo un
        // 500, asi que el handler rechaza la excepcion y no la maneja.
        var handler = new DomainExceptionHandler();
        var context = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("bug"),
            CancellationToken.None);

        handled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context.Response.ContentType.Should().BeNull();
    }

    // Registra un admin con el token de bootstrap, inicia sesion y devuelve el
    // JWT para mandarlo como "Authorization: Bearer ...".
    private static async Task<string> AuthenticateAsync(HttpClient client)
    {
        var email = $"admin{Guid.NewGuid()}@test.com";

        using var registerRequest = new HttpRequestMessage(HttpMethod.Post, "/admin/auth/register");
        registerRequest.Content = JsonContent.Create(new { Email = email, Password = Password });
        registerRequest.Headers.Add("X-Bootstrap-Token", BootstrapToken);

        var registerResponse = await client.SendAsync(registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await client.PostAsJsonAsync(
            "/admin/auth/login",
            new { Email = email, Password = Password });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(loginBody);

        return document.RootElement.GetProperty("token").GetString()!;
    }

    private static async Task<HttpResponseMessage> PostProjectAsync(
        HttpClient client,
        string token,
        string[] technologies,
        string? accept = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["title"] = "Proyecto con tecnologias",
            ["slug"] = $"domain-{Guid.NewGuid():N}",
            ["urlThumbnail"] = "https://example.com/thumb.png",
            ["description"] = "Descripcion del proyecto",
            ["content"] = "# Contenido",
            ["technologies"] = technologies
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/admin/projects")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (accept is not null)
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));

        return await client.SendAsync(request);
    }

    private static async Task<string?> ReadDetailAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        return document.RootElement.GetProperty("detail").GetString();
    }

    // Los nombres de las propiedades del ProblemDetails, en el orden en que
    // aparecen. Sirve para comparar la forma de las dos respuestas.
    private static async Task<List<string>> ReadPropertyNamesAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        return document.RootElement.EnumerateObject().Select(p => p.Name).ToList();
    }
}

// Fabrica con el validador real de la creacion de proyectos reemplazado por
// uno sin reglas: simula la deriva entre el validador y el dominio.
public sealed class DriftedValidatorFactory : ApiWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IValidator<CreateProjectCommand>>();

            services.AddSingleton<IValidator<CreateProjectCommand>, PermissiveCreateProjectValidator>();
        });
    }
}

// Validador sin reglas: deja pasar todo lo que el dominio deberia rechazar.
public sealed class PermissiveCreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
}
