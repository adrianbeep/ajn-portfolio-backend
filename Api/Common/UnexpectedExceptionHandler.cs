using Microsoft.AspNetCore.Diagnostics;

namespace Api.Common;

// Red de seguridad del servidor: cualquier excepcion que el handler de
// dominio decline (un fallo de programacion, no un dato invalido) vuelve al
// cliente como un 500 con ProblemDetails.
//
// El cuerpo se escribe aqui y no con el ProblemDetailsService del
// framework porque ese escritor respeta el header Accept: con
// "Accept: text/html" no escribia nada y el cliente se quedaba con un 500
// pelado, sin cuerpo y sin tipo de contenido.
//
// El detalle no se escribe nunca: el mensaje de la excepcion, su tipo y su
// stack trace son informacion del servidor, no del cliente.
public sealed class UnexpectedExceptionHandler : IExceptionHandler
{
    private const string Title = "An error occurred while processing your request.";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        await ProblemDetailsResponse.WriteAsync(
            httpContext,
            StatusCodes.Status500InternalServerError,
            Title,
            detail: null,
            cancellationToken);

        return true;
    }
}
