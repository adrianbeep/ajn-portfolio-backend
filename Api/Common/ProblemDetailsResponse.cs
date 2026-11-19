using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Common;

// Unico lugar donde se construye y escribe el cuerpo ProblemDetails de la
// API. Los tres caminos que devuelven un error (el normal de la aplicacion,
// la violacion de las reglas de dominio y el fallo inesperado) escriben
// aqui, de modo que el cliente nunca ve dos formatos distintos y el cuerpo
// no depende de lo que diga el header Accept.
//
// La clase es publica (y no internal) para que el proyecto de tests pueda
// fijarla contra el escritor del framework: si el framework cambiara la
// forma del ProblemDetails, el test tiene que avisar.
public static class ProblemDetailsResponse
{
    private const string ContentTypeValue = "application/problem+json";

    // Las mismas opciones que usa el escritor del framework
    // (JsonSerializerDefaults.Web), mas el ignore de nullos: un detail
    // ausente no aparece como "detail": null, sino que no aparece.
    // Una sola instancia para todas las respuestas: las opciones son
    // inmutables una vez construidas.
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    // Escribe el cuerpo ProblemDetails y deja la respuesta con su codigo y su
    // tipo de contenido. El detail puede ser null (eso se omite) y el traceId
    // siempre se escribe: aqui se escribe JSON siempre, sin mirar el Accept.
    public static async Task WriteAsync(
        HttpContext httpContext,
        int status,
        string title,
        string? detail,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = ContentTypeValue;

        var body = new ProblemDetailsBody(
            TypeFor(status),
            title,
            status,
            detail,
            Activity.Current?.Id ?? httpContext.TraceIdentifier);

        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            body,
            SerializerOptions,
            cancellationToken);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return options;
    }

    // Mapa medido contra el framework: el "type" que escribe Results.Problem
    // para cada estado. Un estado que no esta en el mapa no lleva "type",
    // que es lo que hace el framework.
    private static string? TypeFor(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        StatusCodes.Status401Unauthorized => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        StatusCodes.Status403Forbidden => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        StatusCodes.Status500InternalServerError => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
        _ => null
    };

    // Cuerpo del ProblemDetails. El orden de las propiedades es el que ve el
    // cliente: type, title, status, detail, traceId.
    private sealed record ProblemDetailsBody(
        string? Type,
        string Title,
        int Status,
        string? Detail,
        string TraceId);
}
