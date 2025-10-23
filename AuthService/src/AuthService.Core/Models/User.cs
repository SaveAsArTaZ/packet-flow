using System;
using System.Collections.Generic;

namespace AuthService.Core.Models;

/// <summary>
/// Represents a user account in the authentication system.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the username (unique).
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address (unique).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the email has been verified.
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// Gets or sets the hashed password (BCrypt).
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    // Profile Information
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }

    // Account Status
    /// <summary>
    /// Gets or sets whether the account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the account is locked due to security reasons.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Gets or sets when the account lockout will end.
    /// </summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>
    /// Gets or sets the number of consecutive failed login attempts.
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    // OAuth Support
    public string? OAuthProvider { get; set; }
    public string? OAuthId { get; set; }

    // Multi-Factor Authentication
    /// <summary>
    /// Gets or sets whether MFA is enabled for this account.
    /// </summary>
    public bool MfaEnabled { get; set; }

    /// <summary>
    /// Gets or sets the encrypted TOTP secret for MFA.
    /// </summary>
    public string? MfaSecret { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Navigation Properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<AuthLog> AuthLogs { get; set; } = new List<AuthLog>();

    // Computed Properties
    /// <summary>
    /// Gets the full name of the user.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Checks if the account is currently locked.
    /// </summary>
    public bool IsCurrentlyLocked => IsLocked && LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
}


