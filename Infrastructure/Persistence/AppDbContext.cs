using Domain.Entities;
using Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Contribution> Contributions => Set<Contribution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<IdentityRole<Guid>>().HasData(RoleSeed.Data);
        modelBuilder.Entity<Project>().HasData(ProjectSeed.Data);

        // El catálogo de contribuciones arranca vacío: todavía no hay ninguna
        // y el panel de administración que las va a crear es otra pieza.
        //
        // HasData con un arreglo vacío NO es lo mismo que no llamar a HasData:
        // deja en el modelo una anotación de seed vacía que el snapshot no
        // puede representar, el modelo y el snapshot nunca coinciden, y EF
        // aborta el arranque con PendingModelChangesWarning ("the model has
        // pending changes"). Por eso el seed sólo se registra cuando hay algo
        // que sembrar.
        if (ContributionSeed.Data.Length > 0)
        {
            modelBuilder.Entity<Contribution>().HasData(ContributionSeed.Data);
        }
    }
}
