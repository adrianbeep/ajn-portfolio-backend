using System.Diagnostics;
using System.Text.Json;
using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Api.Common;

// Red de seguridad del dominio: traduce una violacion de las reglas de
// negocio a un 400 con ProblemDetails. Si el validador de la capa de
// aplicacion y el dominio se desalinean, el cliente recibe el mismo error
// de campo que habria recibido por el camino normal, en vez de un 500.
public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Solo las violaciones de dominio son un 400. Cualquier otra
        // excepcion es un fallo de programacion y sigue siendo un 500.
        if (exception is not DomainValidationException domainException)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        // El mismo tipo de contenido que emite Results.Problem en el camino
        // normal: una respuesta que se distingue por un parametro suelto no
        // es la misma respuesta.
        httpContext.Response.ContentType = "application/problem+json";

        // El cuerpo se escribe aqui a proposito: el ProblemDetailsService
        // respeta el header Accept y con "Accept: text/html" no escribe nada,
        // dejando un 500 sin cuerpo ni tipo de contenido.
        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            title = "ValidationFailed",
            status = StatusCodes.Status400BadRequest,
            detail = $"{domainException.Field} : {domainException.Message}",
            traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier
        };

        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            problem,
            options: null,
            cancellationToken: cancellationToken);

        return true;
    }
}
