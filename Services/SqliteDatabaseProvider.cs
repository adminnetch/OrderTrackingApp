using Microsoft.EntityFrameworkCore;
using OrderTrackingApp.Models;

namespace OrderTrackingApp.Services;

public class SqliteDatabaseProvider : ISqliteDatabaseProvider
{
    private readonly ILogger<SqliteDatabaseProvider> _logger;

    public SqliteDatabaseProvider(ILogger<SqliteDatabaseProvider> logger)
    {
        _logger = logger;
    }

    public string ProviderName => "SQLite";

    public async Task<bool> InitializeDatabaseAsync(string databasePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created database directory: {Dir}", directory);
            }

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath}");

            using var context = new AppDbContext(optionsBuilder.Options);
            await context.Database.EnsureCreatedAsync();

            _logger.LogInformation("SQLite database initialized: {Path}", databasePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SQLite database: {Path}", databasePath);
            return false;
        }
    }

    public string BuildConnectionString(string databasePath)
    {
        return $"Data Source={databasePath}";
    }

    public async Task<bool> TestConnectionAsync(string databasePath)
    {
        try
        {
            if (!File.Exists(databasePath))
            {
                _logger.LogWarning("Database file not found: {Path}", databasePath);
                return false;
            }

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath}");

            using var context = new AppDbContext(optionsBuilder.Options);
            await context.Database.CanConnectAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQLite connection test failed: {Path}", databasePath);
            return false;
        }
    }

    public IEnumerable<string> GetSchemaScripts()
    {
        return new List<string>
        {
            "-- Users table created by EF Core",
            "-- Migrations handle schema"
        };
    }
}