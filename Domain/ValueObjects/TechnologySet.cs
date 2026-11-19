using Domain.Exceptions;

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

    // Los mensajes de las reglas son del dominio: los validadores de la capa
    // de aplicacion los reutilizan tal cual, de modo que el camino normal (el
    // validador) y el camino de excepcion dicen exactamente lo mismo.
    // Los dos mensajes con numeros se derivan de los limites, nunca escritos
    // a mano, para que no puedan desincronizarse.
    public static class Messages
    {
        public static readonly string TooManyItems =
            $"Technologies must not contain more than {MaxCount} items.";

        public static readonly string EmptyValue =
            "Technologies cannot contain empty values.";

        public static readonly string TooLong =
            $"Technologies items must not exceed {MaxLength} characters.";

        public static readonly string Duplicated =
            "Technologies must not contain duplicates.";
    }

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
            throw new DomainValidationException("Technologies", Messages.TooManyItems);

        var normalized = new List<string>(source.Length);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in source)
        {
            var verdict = Evaluate(raw, seen);

            if (verdict.IsEmpty)
                throw new DomainValidationException("Technologies", Messages.EmptyValue);

            if (verdict.IsTooLong)
                throw new DomainValidationException("Technologies", Messages.TooLong);

            // El primero que aparece gana: se conserva su formato y el orden
            // del autor. Los repetidos se rechazan aunque cambien de formato.
            if (verdict.IsDuplicate)
                throw new DomainValidationException("Technologies", Messages.Duplicated);

            normalized.Add(verdict.Value);
        }

        return new TechnologySet(normalized);
    }

    // El veredicto del dominio sin lanzar excepcion: el validador de la capa
    // de aplicacion lo consulta para devolver un 400 en vez de una excepcion
    // de dominio. Las reglas son las mismas y estan en el mismo sitio, asi
    // que el validador ya no puede copiarlas y desincronizarse.
    // Devuelve las violaciones en el orden en que las ve el cliente: primero
    // la de la coleccion, despues las de cada elemento y al final la de los
    // duplicados, que tambien es una regla de la coleccion entera.
    public static IReadOnlyList<TechnologyViolation> Validate(IEnumerable<string>? technologies)
    {
        if (technologies is null)
            return Array.Empty<TechnologyViolation>();

        var source = technologies.ToArray();
        var violations = new List<TechnologyViolation>();

        if (source.Length > MaxCount)
            violations.Add(new TechnologyViolation(null, Messages.TooManyItems));

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasDuplicates = false;

        for (var index = 0; index < source.Length; index++)
        {
            var verdict = Evaluate(source[index], seen);

            if (verdict.IsEmpty)
                violations.Add(new TechnologyViolation(index, Messages.EmptyValue));

            if (verdict.IsTooLong)
                violations.Add(new TechnologyViolation(index, Messages.TooLong));

            if (verdict.IsDuplicate)
                hasDuplicates = true;
        }

        // Los duplicados se reportan una sola vez y sin indice: son una regla
        // de la coleccion entera, no de un elemento concreto.
        if (hasDuplicates)
            violations.Add(new TechnologyViolation(null, Messages.Duplicated));

        return violations;
    }

    // Normalizar es recortar y colapsar los espacios internos en uno solo.
    // El valor normalizado es el que se compara y el que se persiste.
    // Es publico porque es parte de la API del value object: el veredicto de
    // las reglas se apoya en esta normalizacion y quien lo consulte necesita
    // poder reproducirla.
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var parts = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts);
    }

    // El veredicto de un elemento contra las reglas del dominio. El elemento
    // se registra en "seen" al evaluarlo, asi que el primer caso escrito es el
    // que gana y los repetidos se detectan aunque cambien de formato.
    // Es lo unico que decide si un elemento es valido: las dos caras del
    // dominio (la excepcion y el veredicto) consultan esta misma evaluacion.
    private static ItemVerdict Evaluate(string? raw, HashSet<string> seen)
    {
        var value = Normalize(raw);

        return new ItemVerdict(
            value,
            value.Length == 0,
            value.Length > MaxLength,
            !seen.Add(value));
    }

    private readonly record struct ItemVerdict(
        string Value,
        bool IsEmpty,
        bool IsTooLong,
        bool IsDuplicate);
}
