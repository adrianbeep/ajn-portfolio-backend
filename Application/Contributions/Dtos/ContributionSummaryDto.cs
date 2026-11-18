namespace Application.Contributions.Dtos;

// State y CreatedAt viajan en el resumen a propósito: la tarjeta del listado
// muestra "{año} · {estado}" y, sin ellos, el frontend tenía que pedir el detalle
// de cada contribución sólo para pintar la lista. Eso convertía un fallo de red en
// una tarjeta que desaparece del listado en silencio.
public sealed record ContributionSummaryDto(
    Guid Id,
    string Slug,
    string Title,
    string UrlThumbnail,
    string UrlPrContribution,
    string Description,
    string State,
    DateTime CreatedAt);
