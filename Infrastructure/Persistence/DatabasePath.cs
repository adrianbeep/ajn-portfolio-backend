namespace Infrastructure.Persistence;

/// <summary>
/// Resolves the SQLite connection string used by both the runtime and the EF
/// Core CLI tools (dotnet ef) to a single, stable value that does NOT depend
/// on the process working directory.
///
/// Returns a complete SQLite connection string (not just a path) so callers can
/// pass it straight into UseSqlite(...) or a design-time factory.
///
/// Why this exists:
///   The runtime resolves DataSource= relative to IHostEnvironment.ContentRootPath,
///   while dotnet ef resolves it against Environment.CurrentDirectory. With a
///   relative path like "../Infrastructure/portfolio-dev.db" the two processes
///   land on different files and the bug is silent — both report success.
///   Anchoring the resolution to AppContext.BaseDirectory + the .slnx keeps
///   both processes in lockstep.
///
/// Resolution order:
///   1. PORTFOLIO_DB_PATH environment variable. Returned verbatim — callers may
///      pass any valid SQLite connection string (e.g. "Data Source=:memory:",
///      a shared-cache in-memory string, or a full Data Source=...;Mode=... string).
///   2. Walk up from AppContext.BaseDirectory until we find ajn-portfolio-backend.slnx,
///      then return "Data Source=Infrastructure/portfolio-dev.db" under that root.
/// </summary>
public static class DatabaseConnection
{
    public const string EnvironmentVariable = "PORTFOLIO_DB_PATH";
    private const string SolutionFileName = "ajn-portfolio-backend.slnx";
    private const string DefaultRelativePath = "Infrastructure/portfolio-dev.db";

    public static string Resolve()
    {
        var fromEnv = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            // Return verbatim: this can be a full connection string, not just a path.
            return fromEnv;
        }

        var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
        if (repoRoot is null)
        {
            throw new InvalidOperationException(
                $"Could not locate the repository root (looking for '{SolutionFileName}') " +
                $"starting from '{AppContext.BaseDirectory}'. " +
                $"Set the '{EnvironmentVariable}' environment variable to a SQLite " +
                $"connection string or run from inside the repository.");
        }

        return $"Data Source={Path.Combine(repoRoot, DefaultRelativePath)}";
    }

    private static string? FindRepoRoot(string startDirectory)
    {
        var dir = new DirectoryInfo(startDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, SolutionFileName)))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }
}