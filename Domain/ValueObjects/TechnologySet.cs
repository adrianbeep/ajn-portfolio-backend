namespace Domain.ValueObjects;

// Value object que encapsula las reglas de las tecnologias de un proyecto.
// Es la unica fuente de verdad de esas reglas: la entidad las aplica siempre
// a traves de esta clase, asi que nadie puede guardar un set invalido.
//
// Reglas que quedan aqui:
// - entre 0 y 4 tecnologias (cero es valido: las tecnologias son opcionales)
// - cada tecnologia normalizada tiene que ser no vacia
// - cada tecnologia normalizada tiene que medir 30 caracteres o menos
// - no puede haber repetidas, comparadas sin distinguir mayusculas
// - se conserva el orden del autor y el formato del primer caso escrito
public sealed class TechnologySet
{
    public const int MaxCount = 4;
    public const int MaxLength = 30;

    public static TechnologySet Empty { get; } = new(Array.Empty<string>());

    private readonly IReadOnlyList<string> _items;

    private TechnologySet(IEnumerable<string> items)
        => _items = Array.AsReadOnly(items.ToArray());

    // Copia inmutable: el llamador recibe una foto, no la lista interna.
    public IReadOnlyList<string> Items => _items;

    // null significa "sin tecnologias", que es un estado valido.
    public static TechnologySet Create(IEnumerable<string>? technologies)
    {
        if (technologies is null)
            return Empty;

        var source = technologies.ToArray();

        if (source.Length > MaxCount)
            throw new ArgumentException(
                $"No se pueden declarar mas de {MaxCount} tecnologias (se recibieron {source.Length}).",
                nameof(technologies));

        var normalized = new List<string>(source.Length);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in source)
        {
            var value = Normalize(raw);

            if (value.Length == 0)
                throw new ArgumentException(
                    $"La tecnologia '{Label(raw)}' no puede estar vacia.",
                    nameof(technologies));

            if (value.Length > MaxLength)
                throw new ArgumentException(
                    $"La tecnologia '{value}' supera el maximo de {MaxLength} caracteres ({value.Length}).",
                    nameof(technologies));

            // El primero que aparece gana: se conserva su formato y el orden
            // del autor. Los repetidos se rechazan aunque cambien de formato.
            if (!seen.Add(value))
                throw new ArgumentException(
                    $"La tecnologia '{value}' esta repetida.",
                    nameof(technologies));

            normalized.Add(value);
        }

        return new TechnologySet(normalized);
    }

    // Normalizar es recortar y colapsar los espacios internos en uno solo.
    // El valor normalizado es el que se compara y el que se persiste.
    // Es publico para que los validadores de la capa de aplicacion apliquen
    // exactamente esta normalizacion y no diverjan de estas reglas.
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var parts = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts);
    }

    private static string Label(string? value) => value ?? "<null>";
}
