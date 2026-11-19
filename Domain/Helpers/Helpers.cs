using Domain.Exceptions;

namespace Domain.Helpers;

public static class Helpers
{
    // Los mensajes de las reglas son del dominio: los validadores de la capa
    // de aplicacion los reutilizan tal cual, de modo que el camino normal (el
    // validador) y el camino de excepcion dicen exactamente lo mismo.
    public static class Messages
    {
        public static readonly string TitleRequired = "Title is required.";

        public static readonly string SlugRequired = "Slug is required.";
    }

    internal static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainValidationException("Title", Messages.TitleRequired);
    }

    internal static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainValidationException("Slug", Messages.SlugRequired);
    }
}
