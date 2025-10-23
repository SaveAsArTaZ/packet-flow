using System;
using System.Collections.Generic;

namespace AuthService.Core.Models;

/// <summary>
/// Represents a role that can be assigned to users.
/// </summary>
public class Role
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// The name of the role (e.g., "Admin", "User", "Premium").
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}


