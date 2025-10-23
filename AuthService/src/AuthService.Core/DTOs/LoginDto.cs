using System.ComponentModel.DataAnnotations;

namespace AuthService.Core.DTOs;

/// <summary>
/// Data transfer object for user login.
/// </summary>
public class LoginDto
{
    [Required(ErrorMessage = "Username or email is required")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    /// <summary>
    /// MFA code (required if MFA is enabled for the user).
    /// </summary>
    public string? MfaCode { get; set; }
}

