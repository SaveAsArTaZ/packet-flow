using System.ComponentModel.DataAnnotations;

namespace AuthService.Core.DTOs;

/// <summary>
/// Data transfer object for refresh token requests.
/// </summary>
public class RefreshTokenDto
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}

