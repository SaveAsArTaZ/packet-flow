using System;

namespace AuthService.Core.Models;

/// <summary>
/// Represents an audit log entry for authentication events.
/// </summary>
public class AuthLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    
    /// <summary>
    /// The type of event (e.g., "login", "logout", "login_failed", "password_reset").
    /// </summary>
    public string EventType { get; set; } = string.Empty;
    
    public bool Success { get; set; }

    // Request Information
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Additional Context
    public string? ErrorMessage { get; set; }
    public string? Metadata { get; set; }  // JSON string for additional data

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public User? User { get; set; }
}


