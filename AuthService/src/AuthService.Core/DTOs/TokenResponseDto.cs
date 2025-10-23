namespace AuthService.Core.DTOs;

/// <summary>
/// Response object containing JWT tokens and user information.
/// </summary>
public class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public UserInfoDto User { get; set; } = null!;

    public TokenResponseDto()
    {
    }

    public TokenResponseDto(string accessToken, string refreshToken, DateTime expiresAt, UserInfoDto user)
        : this(accessToken, refreshToken, expiresAt, "Bearer", user)
    {
    }

    public TokenResponseDto(string accessToken, string refreshToken, DateTime expiresAt, string tokenType, UserInfoDto user)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        TokenType = tokenType;
        User = user;
    }
}

