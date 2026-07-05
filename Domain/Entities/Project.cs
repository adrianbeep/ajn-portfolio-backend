using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Project
    {
        public Guid Id { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public string Slug { get; private set; } = string.Empty;
        public string UrlThumbnail { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public string Content { get; private set; } = string.Empty;
        public ProjectState State { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        // Constructor protegido para la hidratación del ORM
        protected Project() { }

        // Constructor para la creación de un nuevo proyecto
        public Project(string title, string slug, string urlThumbnail, string description, string content)
        {
            ValidateStrings(title, slug);

            Id = Guid.NewGuid();
            Title = title;
            Slug = slug;
            UrlThumbnail = urlThumbnail;
            Description = description;
            Content = content;

            // Regla de negocio: Todo proyecto nace activo y registra su creación
            State = ProjectState.Active;
            CreatedAt = DateTime.UtcNow;
        }

        // Métodos de comportamiento para alterar el estado de forma segura
        public void SetInactive()
        {
            if (State == ProjectState.Inactive) return;

            State = ProjectState.Inactive;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetActive()
        {
            if (State == ProjectState.Active) return;

            State = ProjectState.Active;
            UpdatedAt = DateTime.UtcNow;
        }

        private void ValidateStrings(string title, string slug)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentNullException("Title cannot be empty.", nameof(title));

            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentNullException("Slug cannot be empty.", nameof(slug));
        }
    }
}
