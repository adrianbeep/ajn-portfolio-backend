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

        // El cuerpo se escribe aqui a proposito, con el mismo escritor que
        // usa el camino normal: asi el cliente no puede distinguir de donde
        // salio el error, y el body no depende del header Accept.
        await ProblemDetailsResponse.WriteAsync(
            httpContext,
            StatusCodes.Status400BadRequest,
            "ValidationFailed",
            $"{domainException.Field} : {domainException.Message}",
            cancellationToken);

        return true;
    }
}
