using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

/// <summary>
/// Used by `dotnet ef` CLI tools (migrations add, database update, etc.).
///
/// Resolves the SQLite connection string via <see cref="DatabaseConnection"/>
/// so the EF tool and the running API agree on the same database file,
/// regardless of the process working directory.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite(DatabaseConnection.Resolve());

        return new AppDbContext(optionsBuilder.Options);
    }
}