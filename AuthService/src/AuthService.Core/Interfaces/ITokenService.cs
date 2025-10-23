using AuthService.Core.Models;
using System.Security.Claims;

namespace AuthService.Core.Interfaces;

/// <summary>
/// Service interface for JWT token operations.
/// </summary>
public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}
