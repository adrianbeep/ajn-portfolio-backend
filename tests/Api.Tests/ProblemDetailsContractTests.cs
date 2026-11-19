using Application.Projects.Commands.CreateProject;
using Api.Common;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
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

// El cuerpo ProblemDetails de la API sale de un solo escritor
// (ProblemDetailsResponse). Estos tests lo fijan contra el escritor del
// framework (para que un cambio del framework no desaline los dos en
// silencio) y comprueban que el camino de 500 siempre trae cuerpo, sin
// importar el header Accept.
[TestFixture]
public class ProblemDetailsContractTests
{
    private const string BootstrapToken = "bootstrap-token-123";
    private const string Password = "Password123!";

    // Texto interno que nunca puede salir al cliente.
    private const string InternalFailureMessage = "fallo interno de prueba: este texto no puede salir al cliente";

    private static readonly string[] TooManyTechnologies =
        ["Astro", "TypeScript", "EF Core", "SQLite", "Vercel"];

    // (status, title, detail) tal y como los usa la API.
    private static readonly (int Status, string Title, string? Detail)[] ApiCases =
    [
        (400, "ValidationFailed", "Title : Title is required."),
        (401, "Unauthorized", "User unauthorized"),
        (403, "Forbidden", "Admin role required"),
        (404, "ProjectNotFound", "Project not found"),
        (409, "Auth.EmailAlreadyExists", "Email is already registered"),
        (500, "An error occurred while processing your request.", null)
    ];

    [Test]
    public async Task TheSharedWriterSaysTheSameThingAsTheFramework()
    {
        using var provider = BuildServiceProvider();

        foreach (var (status, title, detail) in ApiCases)
        {
            using var framework = JsonDocument.Parse(
                await ReadFrameworkBodyAsync(provider, status, title, detail));
            using var shared = JsonDocument.Parse(
                await ReadSharedBodyAsync(status, title, detail));

            shared.RootElement.GetProperty("type").GetString()
                .Should().Be(framework.RootElement.GetProperty("type").GetString(),
                    $"el type del {status}");

            shared.RootElement.GetProperty("title").GetString()
                .Should().Be(framework.RootElement.GetProperty("title").GetString(),
                    $"el title del {status}");

            shared.RootElement.GetProperty("status").GetInt32()
                .Should().Be(framework.RootElement.GetProperty("status").GetInt32(),
                    $"el status del {status}");

            // Un detail null no se escribe: ni como null ni como cadena vacia.
            if (detail is null)
            {
                shared.RootElement.TryGetProperty("detail", out _).Should().BeFalse(
                    $"el detail null del {status} no puede aparecer");
                framework.RootElement.TryGetProperty("detail", out _).Should().BeFalse(
                    $"el detail null del {status} no puede aparecer");
            }
            else
            {
                shared.RootElement.GetProperty("detail").GetString()
                    .Should().Be(framework.RootElement.GetProperty("detail").GetString(),
                        $"el detail del {status}");
            }

            // El orden de las propiedades tambien es parte del contrato.
            ReadPropertyNames(shared.RootElement).Should().Equal(
                ReadPropertyNames(framework.RootElement),
                $"el orden de las propiedades del {status}");
        }
    }

    [Test]
    public async Task TheNormalPathAndTheDomainPathSayTheSameThing()
    {
        // Camino normal: el validador real rechaza antes de llegar al dominio.
        await using var normalFactory = new ApiWebApplicationFactory();
        using var normalClient = normalFactory.CreateClient();
        var normalToken = await AuthenticateAsync(normalClient);

        // Camino de excepcion: el validador deja pasar el dato y lo rechaza el
        // dominio. Los dos caminos tienen que decir exactamente lo mismo.
        await using var driftedFactory = new DriftedValidatorFactory();
        using var driftedClient = driftedFactory.CreateClient();
        var driftedToken = await AuthenticateAsync(driftedClient);

        var normal = await PostProjectAsync(normalClient, normalToken, TooManyTechnologies);
        var drifted = await PostProjectAsync(driftedClient, driftedToken, TooManyTechnologies);

        normal.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        drifted.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        normal.Content.Headers.ContentType!.ToString()
            .Should().Be(drifted.Content.Headers.ContentType!.ToString());

        using var normalProblem = JsonDocument.Parse(await normal.Content.ReadAsStringAsync());
        using var driftedProblem = JsonDocument.Parse(await drifted.Content.ReadAsStringAsync());

        WithoutTraceId(normalProblem.RootElement)
            .Should().Be(WithoutTraceId(driftedProblem.RootElement));
    }

    [Test]
    public async Task AnUnexpectedFailureAlwaysComesBackWithABody()
    {
        await using var factory = new ThrowingValidatorFactory();
        using var client = factory.CreateClient();
        var token = await AuthenticateAsync(client);

        var plain = await PostProjectAsync(client, token, ["Astro"], accept: null);
        var html = await PostProjectAsync(client, token, ["Astro"], accept: "text/html");

        plain.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        html.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        // El tipo de contenido tampoco depende del header Accept.
        plain.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        html.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var plainBody = await plain.Content.ReadAsStringAsync();
        var htmlBody = await html.Content.ReadAsStringAsync();

        plainBody.Should().NotBeNullOrWhiteSpace();
        htmlBody.Should().NotBeNullOrWhiteSpace();

        using var plainProblem = JsonDocument.Parse(plainBody);
        using var htmlProblem = JsonDocument.Parse(htmlBody);

        plainProblem.RootElement.GetProperty("title").GetString()
            .Should().Be("An error occurred while processing your request.");
        htmlProblem.RootElement.GetProperty("title").GetString()
            .Should().Be("An error occurred while processing your request.");

        plainProblem.RootElement.GetProperty("status").GetInt32().Should().Be(500);
        htmlProblem.RootElement.GetProperty("status").GetInt32().Should().Be(500);

        // Sin detail: el cliente no se entera de nada del fallo.
        plainProblem.RootElement.TryGetProperty("detail", out _).Should().BeFalse();
        htmlProblem.RootElement.TryGetProperty("detail", out _).Should().BeFalse();

        // Ni el mensaje de la excepcion, ni su tipo, ni el stack trace.
        plainBody.Should().NotContain(InternalFailureMessage);
        htmlBody.Should().NotContain(InternalFailureMessage);
        plainBody.Should().NotContain(nameof(InvalidOperationException));
        htmlBody.Should().NotContain(nameof(InvalidOperationException));

        // El traceId siempre esta (a diferencia del escritor del framework con
        // "Accept: text/html") y el resto del cuerpo es el mismo.
        plainProblem.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
        htmlProblem.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();

        WithoutTraceId(plainProblem.RootElement).Should().Be(WithoutTraceId(htmlProblem.RootElement));
    }

    // El framework escribe el ProblemDetails con su propio escritor, que
    // necesita un contenedor de servicios con AddProblemDetails().
    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddProblemDetails();

        return services.BuildServiceProvider();
    }

    private static async Task<string> ReadFrameworkBodyAsync(
        IServiceProvider provider,
        int status,
        string title,
        string? detail)
    {
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Response.Body = new MemoryStream();

        await Results.Problem(statusCode: status, title: title, detail: detail)
            .ExecuteAsync(context);

        return await ReadBodyAsync(context);
    }

    private static async Task<string> ReadSharedBodyAsync(int status, string title, string? detail)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await ProblemDetailsResponse.WriteAsync(context, status, title, detail, CancellationToken.None);

        context.Response.StatusCode.Should().Be(status);
        context.Response.ContentType.Should().Be("application/problem+json");

        return await ReadBodyAsync(context);
    }

    private static async Task<string> ReadBodyAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;

        using var reader = new StreamReader(context.Response.Body);

        return await reader.ReadToEndAsync();
    }

    // El traceId cambia en cada peticion: para comparar dos cuerpos se
    // comparan las demas propiedades, en su orden.
    private static string WithoutTraceId(JsonElement problem) =>
        string.Join(",", problem.EnumerateObject()
            .Where(p => p.Name != "traceId")
            .Select(p => $"\"{p.Name}\":{p.Value.GetRawText()}"));

    private static List<string> ReadPropertyNames(JsonElement problem) =>
        problem.EnumerateObject().Select(p => p.Name).ToList();

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
            ["slug"] = $"contract-{Guid.NewGuid():N}",
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
}

// Fabrica que sustituye el validador real por uno que revienta: simula un
// fallo de programacion en el camino normal de la peticion.
public sealed class ThrowingValidatorFactory : ApiWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IValidator<CreateProjectCommand>>();

            services.AddSingleton<IValidator<CreateProjectCommand>, ThrowingCreateProjectValidator>();
        });
    }
}

// Validador que revienta en vez de validar: la excepcion se escapa del
// pipeline de MediatR y llega al middleware de excepcion de la API.
public sealed class ThrowingCreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public const string FailureMessage =
        "fallo interno de prueba: este texto no puede salir al cliente";

    public override Task<ValidationResult> ValidateAsync(
        ValidationContext<CreateProjectCommand> context,
        CancellationToken cancellation = default)
    {
        throw new InvalidOperationException(FailureMessage);
    }
}
