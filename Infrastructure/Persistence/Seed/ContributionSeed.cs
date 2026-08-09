using Domain.Entities;

namespace Infrastructure.Persistence.Seed;

public static class ContributionSeed
{
    public static readonly Contribution[] Data = new[]
    {
        new Contribution(
            id: new Guid("22222222-2222-2222-2222-222222222222"),
            createdAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            title: "Mi Portfolio",
            slug: "mi-portfolio",
            urlThumbnail: "https://example.com/img.png",
            urlPrContribution: "https://example.com/img.png",
            description: "Sitio web de portfolio personal",
            content: "Contenido del proyecto de portfolio..."),

        new Contribution(
            id: new Guid("33333333-3333-3333-3333-333333333333"),
            createdAt: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            title: "API de Gestión",
            slug: "api-gestion",
            urlThumbnail: "https://example.com/api.png",
            urlPrContribution: "https://example.com/api.png",
            description: "API REST para gestión de proyectos",
            content: "Documentación y endpoints del proyecto...")
    };
}
