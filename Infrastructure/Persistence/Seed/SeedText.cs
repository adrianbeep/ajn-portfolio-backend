namespace Infrastructure.Persistence.Seed;

// El contenido sembrado llega desde raw string literals, y el compilador
// embebe los saltos de linea tal cual estan en el archivo fuente. Con
// core.autocrlf=true el working tree en Windows tiene CRLF y el checkout de
// Linux (CI) tiene LF, asi que el mismo codigo produce dos valores distintos.
// EF compara el seed contra el snapshot byte a byte y aborta el arranque con
// PendingModelChangesWarning ("the model has pending changes"). Normalizar
// aqui hace que el valor sea identico en cualquier plataforma.
public static class SeedText
{
    public static string Normalize(string value)
        => value.Replace("\r\n", "\n").Replace("\r", "\n");
}
