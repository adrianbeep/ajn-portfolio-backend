using System.Text.Json;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    // Una sola instancia compartida para todo el mapeo: crear las opciones en
    // cada llamada genera trabajo en cada lectura y cada escritura.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(120);
        
        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(80);
        
        builder.Property(p => p.UrlThumbnail)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(p => p.Content)
            .HasColumnType("text");

        builder.Property(p => p.State)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Las tecnologias se guardan como un arreglo JSON en una columna TEXT.
        // El tipo de columna no se fija a proposito: el que decide el proveedor
        // (TEXT en SQLite, el que corresponda en Postgres).
        builder.Property(p => p.Technologies)
            .HasConversion(
                technologies => JsonSerializer.Serialize(technologies, JsonOptions),
                json => Deserialize(json))
            .Metadata.SetValueComparer(TechnologyListComparer.Instance);

        builder.HasIndex(p => p.Slug)
            .IsUnique();

        builder.HasQueryFilter(p => p.DeletedAt == null);
    }

    // La columna se agrega a una tabla que ya tiene filas: el valor heredado
    // puede llegar vacio o null, y en ese caso el proyecto se lee sin
    // tecnologias, nunca con null.
    private static IReadOnlyList<string> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<string>();

        var items = JsonSerializer.Deserialize<string[]>(json, JsonOptions);
        return items is null ? Array.Empty<string>() : Array.AsReadOnly(items);
    }

    // IReadOnlyList<string> no es comparable: sin un comparador explicito EF
    // aborta al seguir los cambios con "Unable to compare values of type
    // 'System.Collections.Generic.IReadOnlyList`1[System.String]'".
    private sealed class TechnologyListComparer : ValueComparer<IReadOnlyList<string>>
    {
        public static readonly TechnologyListComparer Instance = new();

        public TechnologyListComparer()
            : base(
                (left, right) => left != null && right != null && left.SequenceEqual(right, StringComparer.Ordinal),
                technologies => technologies == null ? 0 : Hash(technologies),
                technologies => technologies == null ? Array.Empty<string>() : technologies.ToArray())
        {
        }

        private static int Hash(IReadOnlyList<string> technologies)
        {
            var hash = 17;

            foreach (var technology in technologies)
                hash = HashCode.Combine(hash, technology is null ? 0 : StringComparer.Ordinal.GetHashCode(technology));

            return hash;
        }
    }
}
