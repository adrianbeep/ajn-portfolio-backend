namespace Api.Common;

// Resultado que escribe un ProblemDetails con el escritor de la API. Existe
// para que el camino normal de error (el de la aplicacion) use el mismo
// cuerpo que el camino de excepcion de dominio y que el de fallo
// inesperado, en vez de depender de Results.Problem y del
// ProblemDetailsService (que respetan el header Accept).
internal sealed class ProblemDetailsResult : IResult
{
    private readonly int _status;
    private readonly string _title;
    private readonly string? _detail;

    public ProblemDetailsResult(int status, string title, string? detail)
    {
        _status = status;
        _title = title;
        _detail = detail;
    }

    public Task ExecuteAsync(HttpContext httpContext) =>
        ProblemDetailsResponse.WriteAsync(
            httpContext,
            _status,
            _title,
            _detail,
            httpContext.RequestAborted);
}
