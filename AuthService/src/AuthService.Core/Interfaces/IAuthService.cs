using AuthService.Core.DTOs;

namespace AuthService.Core.Interfaces;

/// <summary>
/// Service interface for authentication operations.
/// </summary>
public interface IAuthService
{
    Task<TokenResponseDto> RegisterAsync(RegisterDto dto, string ipAddress, string userAgent);
    Task<TokenResponseDto> LoginAsync(LoginDto dto, string ipAddress, string userAgent);
    Task<TokenResponseDto> RefreshTokenAsync(string refreshToken, string ipAddress, string userAgent);
    Task RevokeTokenAsync(string refreshToken, string ipAddress);
    Task<bool> VerifyEmailAsync(string token);
    Task RequestPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
}
