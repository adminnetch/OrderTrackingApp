using Microsoft.EntityFrameworkCore;
using OrderTrackingApp.Models;

namespace OrderTrackingApp.Services;

public class InstallationStateService : IInstallationStateService
{
    private readonly AppDbContext _context;
    private readonly ILogger<InstallationStateService> _logger;

    public InstallationStateService(AppDbContext context, ILogger<InstallationStateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<InstallationState> GetCurrentStateAsync()
    {
        try
        {
            if (!await _context.Database.CanConnectAsync())
                return InstallationState.NotStarted;
            
            if (!await _context.AppInstallations.AnyAsync())
                return InstallationState.NotStarted;
            
            var installation = await _context.AppInstallations.FirstOrDefaultAsync();
            
            if (installation == null || string.IsNullOrEmpty(installation.CurrentState))
                return InstallationState.NotStarted;
            
            if (Enum.TryParse<InstallationState>(installation.CurrentState, out var state))
                return state;
            
            return InstallationState.NotStarted;
        }
        catch (Exception)
        {
            return InstallationState.NotStarted;
        }
    }

    public async Task UpdateStateAsync(InstallationState newState)
    {
        var installation = await GetOrCreateInstallationAsync();
        
        installation.PreviousState = installation.CurrentState;
        installation.CurrentState = newState.ToString();
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Installation state changed: {Previous} -> {New}",
            installation.PreviousState, newState);
    }

    public async Task MarkCompleteAsync()
    {
        var installation = await GetOrCreateInstallationAsync();
        
        installation.CurrentState = InstallationState.Complete.ToString();
        installation.CompletedDate = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Installation completed at {Date}", installation.CompletedDate);
    }

    public async Task<bool> CanAccessAsync(string sessionId)
    {
        var installation = await GetOrCreateInstallationAsync();
        
        if (!installation.IsLocked)
            return true;
        
        if (installation.LockedBySessionId.HasValue && 
            installation.LockedBySessionId == Guid.Parse(sessionId))
            return true;
        
        if (installation.LockedAt.HasValue)
        {
            var lockDuration = DateTime.UtcNow - installation.LockedAt.Value;
            if (lockDuration.TotalMinutes > 30)
            {
                installation.IsLocked = false;
                installation.LockedAt = null;
                installation.LockedBySessionId = null;
                await _context.SaveChangesAsync();
                return true;
            }
        }
        
        return false;
    }

    public async Task<string?> GetSetupSessionTokenAsync()
    {
        var installation = await GetOrCreateInstallationAsync();
        return installation.InstallationId.ToString();
    }

    public async Task<bool> IsFirstRunRequiredAsync()
    {
        var state = await GetCurrentStateAsync();
        return state != InstallationState.Complete;
    }

    private async Task<AppInstallation> GetOrCreateInstallationAsync()
    {
        var installation = await _context.AppInstallations.FirstOrDefaultAsync();
        
        if (installation == null)
        {
            installation = new AppInstallation();
            _context.AppInstallations.Add(installation);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("New installation created: {Id}", installation.InstallationId);
        }
        
        return installation;
    }
}