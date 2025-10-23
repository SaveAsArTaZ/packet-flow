using System;

namespace AuthService.Core.Models;

/// <summary>
/// Represents a refresh token used for obtaining new access tokens.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    /// <summary>
    /// The actual token string (should be cryptographically secure random).
    /// </summary>
    public string Token { get; set; } = string.Empty;

    // Token Lifecycle
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public bool IsRevoked { get; set; }
    
    /// <summary>
    /// If this token was replaced (during refresh), stores the new token.
    /// </summary>
    public string? ReplacedByToken { get; set; }

    // Device/Client Information
    public string? DeviceName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Navigation Properties
    public User User { get; set; } = null!;

    // Computed Properties
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}


