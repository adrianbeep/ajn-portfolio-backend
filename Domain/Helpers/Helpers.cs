namespace Domain.Helpers;

internal static class Helpers
{
    internal static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentNullException("Title cannot be empty.", nameof(title));
    }

    internal static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentNullException("Slug cannot be empty.", nameof(slug));
    }
}
