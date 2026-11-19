using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities;

public class Project
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string UrlThumbnail { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public State State { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Las tecnologias son etiquetas libres: entre 0 y 4, sin repetidas y ya
    // normalizadas. La lista vacia es un estado valido.
    public IReadOnlyList<string> Technologies { get; private set; } = Array.Empty<string>();

    // Constructor protegido para la hidratación del ORM
    protected Project() { }

    // Constructor para la creación de un nuevo proyecto
    public Project(
        string title,
        string slug,
        string urlThumbnail,
        string description,
        string content,
        IReadOnlyList<string>? technologies = null)
    {
        Helpers.Helpers.ValidateTitle(title);
        Helpers.Helpers.ValidateSlug(slug);

        Id = Guid.NewGuid();
        Title = title;
        Slug = slug;
        UrlThumbnail = urlThumbnail;
        Description = description;
        Content = content;

        // Las reglas viven en el value object: la entidad no las reimplementa.
        Technologies = TechnologySet.Create(technologies).Items;

        // Regla de negocio: Todo proyecto nace activo y registra su creación
        State = State.Active;
        CreatedAt = DateTime.UtcNow;
    }
    
    // Constructor para seeds/migraciones — solo Infrastructure lo usa.
    public Project(
        Guid id,
        DateTime createdAt,
        string title,
        string slug,
        string urlThumbnail,
        string description,
        string content,
        IReadOnlyList<string>? technologies = null)
    {
        Helpers.Helpers.ValidateTitle(title);
        Helpers.Helpers.ValidateSlug(slug);

        Id = id;
        CreatedAt = createdAt;
        Title = title;
        Slug = slug;
        UrlThumbnail = urlThumbnail;
        Description = description;
        Content = content;

        // Las reglas viven en el value object: la entidad no las reimplementa.
        Technologies = TechnologySet.Create(technologies).Items;

        State = State.Active;
    }
    
    public void MarkAsDeleted()
    {
        if (DeletedAt is not null) return;     // idempotente
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (DeletedAt is null) return;         // idempotente
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        string title,
        string description,
        string urlThumbnail,
        string content,
        IReadOnlyList<string>? technologies = null)
    {
        Helpers.Helpers.ValidateTitle(title);
        
        Title = title;
        Description = description;
        UrlThumbnail = urlThumbnail;
        Content = content;
        // Las reglas viven en el value object: la entidad no las reimplementa.
        Technologies = TechnologySet.Create(technologies).Items;
        UpdatedAt = DateTime.UtcNow;
    }
}
