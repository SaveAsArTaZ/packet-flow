using System;

namespace AuthService.Core.Models;

/// <summary>
/// Junction table for many-to-many relationship between Users and Roles.
/// </summary>
public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}


