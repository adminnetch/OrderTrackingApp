using System.ComponentModel.DataAnnotations;

namespace OrderTrackingApp.Models;

public class AppInstallation
{
    [Key]
    public Guid InstallationId { get; set; } = Guid.NewGuid();
    
    public DateTime InstallationDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedDate { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string CurrentState { get; set; } = InstallationState.NotStarted.ToString();
    
    public string? PreviousState { get; set; }
    
    [MaxLength(20)]
    public string DatabaseProvider { get; set; } = "sqlite";
    
    [MaxLength(255)]
    public string? DatabasePath { get; set; }
    
    public bool IsLocked { get; set; }
    
    public DateTime? LockedAt { get; set; }
    
    public Guid? LockedBySessionId { get; set; }
    
    public string? LastErrorMessage { get; set; }
    
    public string? InstallationProfile { get; set; } = "express";
}

public enum InstallationState
{
    NotStarted,
    PrerequisitesValidated,
    DatabaseConfigured,
    DatabaseInitialized,
    SuperadminCreated,
    Complete,
    Failed
}