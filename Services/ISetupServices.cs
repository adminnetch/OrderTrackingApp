using OrderTrackingApp.Models;

namespace OrderTrackingApp.Services;

public interface IInstallationStateService
{
    Task<InstallationState> GetCurrentStateAsync();
    Task UpdateStateAsync(InstallationState newState);
    Task MarkCompleteAsync();
    Task<bool> CanAccessAsync(string sessionId);
    Task<string?> GetSetupSessionTokenAsync();
    Task<bool> IsFirstRunRequiredAsync();
}

public interface ISqliteDatabaseProvider
{
    string ProviderName { get; }
    Task<bool> InitializeDatabaseAsync(string databasePath);
    string BuildConnectionString(string databasePath);
    Task<bool> TestConnectionAsync(string databasePath);
    IEnumerable<string> GetSchemaScripts();
}

public interface ISuperadminService
{
    Task<(bool Success, List<string> Errors)> CreateSuperadminAsync(string username, string email, string password);
}