using Domain.Enums;

namespace Domain.Entities;

public class Contribution
{
    public Guid Id { get; set; }
    public string Title { get; private set; }
    public string Slug { get; private set; }
    public string UrlThumbnail { get; private set; }
    public string UrlPrContribution { get; private set; }
    public string Description { get; private set; }
    public string Content { get; private set; }
    public State State { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    protected Contribution() { }

    public Contribution(
        string title,
        string slug,
        string urlThumbnail,
        string urlPrContribution,
        string description,
        string content)
    {
        Helpers.Helpers.ValidateTitle(title);
        Helpers.Helpers.ValidateSlug(slug);

        Id = Guid.NewGuid();
        Title = title;
        Slug = slug;
        UrlThumbnail = urlThumbnail;
        UrlPrContribution = urlPrContribution;
        Description = description;
        Content = content;
        
        //Regla de negocio: Todo contribution nace activo y registra su creacion
        State = State.Active;
        CreatedAt = DateTime.UtcNow;
    }
    
    //Constructor para seeds/migraciones — solo Infrastructure lo usa.
    public Contribution(
        Guid id,
        DateTime createdAt,
        string title,
        string slug,
        string urlThumbnail,
        string urlPrContribution,
        string description,
        string content)
    {
        Helpers.Helpers.ValidateTitle(title);
        Helpers.Helpers.ValidateSlug(slug);

        Id = id;
        CreatedAt = createdAt;
        Title = title;
        Slug = slug;
        UrlThumbnail = urlThumbnail;
        UrlPrContribution = urlPrContribution;
        Description = description;
        Content = content;

        State = State.Active;
    }

    public void MarkAsDeleted()
    {
        if (DeletedAt is not null) return;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (DeletedAt is null) return;
        DeletedAt = null;
        UpdatedAt = null;
    }

    public void Update(
        string title,
        string description,
        string urlThumbnail,
        string urlPrContribution,
        string content)
    {
        Helpers.Helpers.ValidateTitle(title);

        Title = title;
        Description = description;
        UrlThumbnail = urlThumbnail;
        UrlPrContribution = urlPrContribution;
        Content = content;
        UpdatedAt = DateTime.UtcNow;
    }
}
