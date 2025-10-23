using AuthService.Core.DTOs;
using AuthService.Core.Exceptions;
using AuthService.Core.Interfaces;
using AuthService.Core.Models;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Services;

/// <summary>
/// Main authentication service implementation.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AuthDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AuthDbContext context,
        IUserRepository userRepository,
        ITokenService tokenService,
        IPasswordService passwordService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task<TokenResponseDto> RegisterAsync(RegisterDto dto, string ipAddress, string userAgent)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", dto.Email);

        // Validate password
        var (isValid, error) = _passwordService.ValidatePassword(dto.Password);
        if (!isValid)
            throw new ValidationException(error!);

        // Check if password and confirm password match
        if (dto.Password != dto.ConfirmPassword)
            throw new ValidationException("Passwords do not match");

        // Check if email already exists
        if (await _userRepository.ExistsAsync(u => u.Email == dto.Email))
        {
            _logger.LogWarning("Registration failed: Email already exists - {Email}", dto.Email);
            throw new ValidationException("Email already registered");
        }

        // Check if username already exists
        if (await _userRepository.ExistsAsync(u => u.Username == dto.Username))
        {
            _logger.LogWarning("Registration failed: Username already taken - {Username}", dto.Username);
            throw new ValidationException("Username already taken");
        }

        // Hash password
        var passwordHash = _passwordService.HashPassword(dto.Password);

        // Create user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = passwordHash,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);

        // Assign default "User" role
        var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (userRole != null)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = userRole.Id,
                AssignedAt = DateTime.UtcNow
            });
        }

        await _userRepository.SaveChangesAsync();

        // Log the registration
        await LogAuthEventAsync(user.Id, "registration", true, ipAddress, userAgent);

        _logger.LogInformation("User registered successfully: {UserId}", user.Id);

        // Generate tokens
        return await GenerateTokenResponseAsync(user, ipAddress, userAgent);
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto dto, string ipAddress, string userAgent)
    {
        _logger.LogInformation("Login attempt for: {UsernameOrEmail}", dto.UsernameOrEmail);

        // Find user
        var user = await _userRepository.GetByUsernameOrEmailAsync(dto.UsernameOrEmail);

        if (user == null)
        {
            await LogAuthEventAsync(null, "login_failed", false, ipAddress, userAgent, "User not found");
            _logger.LogWarning("Login failed: User not found - {UsernameOrEmail}", dto.UsernameOrEmail);
            throw new UnauthorizedException("Invalid credentials");
        }

        // Check if account is locked
        if (user.IsLocked && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            await LogAuthEventAsync(user.Id, "login_failed", false, ipAddress, userAgent, "Account locked");
            _logger.LogWarning("Login failed: Account locked - {UserId}", user.Id);
            throw new AccountLockedException(user.LockoutEnd.Value);
        }

        // Verify password
        if (!_passwordService.VerifyPassword(dto.Password, user.PasswordHash))
        {
            // Increment failed attempts
            user.FailedLoginAttempts++;

            // Lock account after 5 failed attempts
            if (user.FailedLoginAttempts >= 5)
            {
                user.IsLocked = true;
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
                _logger.LogWarning("Account locked due to failed attempts: {UserId}", user.Id);
            }

            await _userRepository.SaveChangesAsync();
            await LogAuthEventAsync(user.Id, "login_failed", false, ipAddress, userAgent, "Invalid password");
            
            throw new UnauthorizedException("Invalid credentials");
        }

        // Check MFA if enabled
        if (user.MfaEnabled && string.IsNullOrEmpty(dto.MfaCode))
        {
            await LogAuthEventAsync(user.Id, "mfa_required", false, ipAddress, userAgent);
            throw new ValidationException("MFA code required");
        }

        // Reset failed login attempts
        user.FailedLoginAttempts = 0;
        user.IsLocked = false;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;

        await _userRepository.SaveChangesAsync();

        // Log successful login
        await LogAuthEventAsync(user.Id, "login", true, ipAddress, userAgent);
        _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

        // Generate tokens
        return await GenerateTokenResponseAsync(user, ipAddress, userAgent, dto.RememberMe);
    }

    public async Task<TokenResponseDto> RefreshTokenAsync(string refreshToken, string ipAddress, string userAgent)
    {
        _logger.LogInformation("Token refresh attempt");

        // Find the refresh token
        var token = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token == null)
        {
            _logger.LogWarning("Refresh token not found");
            throw new UnauthorizedException("Invalid refresh token");
        }

        // Validate token
        if (!token.IsActive)
        {
            await LogAuthEventAsync(token.UserId, "refresh_token_failed", false, ipAddress, userAgent, "Token expired or revoked");
            _logger.LogWarning("Refresh token is not active: {UserId}", token.UserId);
            throw new UnauthorizedException("Invalid refresh token");
        }

        // Revoke old token
        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;

        // Create new refresh token
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        token.ReplacedByToken = newRefreshToken;

        var newToken = new RefreshToken
        {
            UserId = token.UserId,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        _context.RefreshTokens.Add(newToken);
        await _context.SaveChangesAsync();

        // Generate new access token
        var accessToken = _tokenService.GenerateAccessToken(token.User);

        await LogAuthEventAsync(token.UserId, "token_refreshed", true, ipAddress, userAgent);
        _logger.LogInformation("Token refreshed successfully: {UserId}", token.UserId);

        return new TokenResponseDto(
            accessToken,
            newRefreshToken,
            DateTime.UtcNow.AddMinutes(15),
            MapToUserInfo(token.User)
        );
    }

    public async Task RevokeTokenAsync(string refreshToken, string ipAddress)
    {
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token == null)
            return;

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await LogAuthEventAsync(token.UserId, "token_revoked", true, ipAddress, "");

        _logger.LogInformation("Token revoked: {UserId}", token.UserId);
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        // Implementation would require EmailVerificationToken entity
        // For now, return a placeholder
        await Task.CompletedTask;
        return true;
    }

    public async Task RequestPasswordResetAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        
        if (user == null)
        {
            // Don't reveal if email exists or not
            _logger.LogWarning("Password reset requested for non-existent email");
            return;
        }

        // Implementation would require PasswordResetToken entity and email service
        _logger.LogInformation("Password reset requested for: {UserId}", user.Id);
        
        await Task.CompletedTask;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        // Implementation would require PasswordResetToken entity
        // For now, return a placeholder
        await Task.CompletedTask;
        return true;
    }

    private async Task<TokenResponseDto> GenerateTokenResponseAsync(
        User user,
        string ipAddress,
        string userAgent,
        bool rememberMe = false)
    {
        // Generate JWT access token
        var accessToken = _tokenService.GenerateAccessToken(user);

        // Generate refresh token
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Store refresh token
        var tokenExpiry = rememberMe 
            ? DateTime.UtcNow.AddDays(30) 
            : DateTime.UtcNow.AddDays(7);

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = tokenExpiry,
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        await _context.SaveChangesAsync();

        return new TokenResponseDto(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(15),
            MapToUserInfo(user)
        );
    }

    private UserInfoDto MapToUserInfo(User user)
    {
        return new UserInfoDto(
            user.Id,
            user.Username,
            user.Email,
            user.EmailVerified,
            user.FirstName,
            user.LastName,
            user.AvatarUrl,
            user.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.Role.Name).ToList(),
            user.MfaEnabled
        );
    }

    private async Task LogAuthEventAsync(
        Guid? userId,
        string eventType,
        bool success,
        string ipAddress,
        string userAgent,
        string? errorMessage = null)
    {
        _context.AuthLogs.Add(new AuthLog
        {
            UserId = userId,
            EventType = eventType,
            Success = success,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ErrorMessage = errorMessage,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }
}

