namespace Domain.Exceptions;

// Excepcion de dominio para una violacion de las reglas de negocio.
// Se distingue de un fallo de programacion: el dato es invalido, no el
// codigo. La capa de API la traduce a un 400 con ProblemDetails.
public sealed class DomainValidationException : Exception
{
    public DomainValidationException(string field, string message)
        : base(message)
    {
        Field = field;
    }

    // El campo del contrato al que corresponde el error ("Technologies", "Title", "Slug").
    public string Field { get; }
}
