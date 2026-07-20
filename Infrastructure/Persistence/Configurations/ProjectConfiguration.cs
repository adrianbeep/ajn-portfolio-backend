using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
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

        builder.HasIndex(p => p.Slug)
            .IsUnique();

        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}
