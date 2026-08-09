using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ContributionConfiguration : IEntityTypeConfiguration<Contribution>
{
    public void Configure(EntityTypeBuilder<Contribution> builder)
    {
        builder.ToTable("contributions");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(120);
        
        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(80);
        
        builder.Property(c => c.UrlThumbnail)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.UrlPrContribution)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(c => c.Content)
            .HasColumnType("text");

        builder.Property(c => c.State)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(c => c.Slug)
            .IsUnique();

        builder.HasQueryFilter(c => c.DeletedAt == null);
    }
}
