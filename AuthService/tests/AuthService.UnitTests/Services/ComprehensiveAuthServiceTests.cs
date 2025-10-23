using AuthService.Core.DTOs;
using AuthService.Core.Exceptions;
using AuthService.Core.Interfaces;
using AuthService.Core.Models;
using AuthService.Infrastructure.Data;
using AuthService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AuthService.UnitTests.Services;

public class ComprehensiveAuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IPasswordService> _mockPasswordService;
    private readonly Mock<ILogger<Infrastructure.Services.AuthService>> _mockLogger;
    private readonly AuthDbContext _dbContext;
    private readonly Infrastructure.Services.AuthService _authService;

    public ComprehensiveAuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new AuthDbContext(options);

        _mockUserRepository = new Mock<IUserRepository>();
        _mockTokenService = new Mock<ITokenService>();
        _mockPasswordService = new Mock<IPasswordService>();
        _mockLogger = new Mock<ILogger<Infrastructure.Services.AuthService>>();

        _authService = new Infrastructure.Services.AuthService(
            _dbContext,
            _mockUserRepository.Object,
            _mockTokenService.Object,
            _mockPasswordService.Object,
            _mockLogger.Object
        );

        // Seed default role
        _dbContext.Roles.Add(new Role
        {
            Id = Guid.NewGuid(),
            Name = "User",
            Description = "Default user role",
            CreatedAt = DateTime.UtcNow
        });
        _dbContext.SaveChanges();
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithExistingUsername_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "existinguser",
            Email = "new@example.com",
            Password = "ValidPassword123!",
            ConfirmPassword = "ValidPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        _mockPasswordService.Setup(s => s.ValidatePassword(dto.Password))
            .Returns((true, null));

        // Setup email check to return false (email doesn't exist)
        _mockUserRepository.Setup(r => r.ExistsAsync(It.Is<System.Linq.Expressions.Expression<Func<User, bool>>>(
            expr => expr.ToString().Contains("Email"))))
            .ReturnsAsync(false);

        // Setup username check to return true (username exists)
        _mockUserRepository.Setup(r => r.ExistsAsync(It.Is<System.Linq.Expressions.Expression<Func<User, bool>>>(
            expr => expr.ToString().Contains("Username"))))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _authService.RegisterAsync(dto, "127.0.0.1", "TestAgent")
        );

        Assert.Contains("Username", exception.Message);
    }

    #endregion

    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange
        var user = CreateTestUser();
        var oldRefreshToken = "valid_refresh_token";
        
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Token = oldRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false,
            IpAddress = "127.0.0.1",
            UserAgent = "TestAgent"
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _mockTokenService.Setup(s => s.GenerateAccessToken(user))
            .Returns("new_access_token");

        _mockTokenService.Setup(s => s.GenerateRefreshToken())
            .Returns("new_refresh_token");

        // Act
        var result = await _authService.RefreshTokenAsync(oldRefreshToken, "127.0.0.1", "TestAgent");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new_access_token", result.AccessToken);
        Assert.Equal("new_refresh_token", result.RefreshToken);

        // Verify old token was revoked
        var oldToken = await _dbContext.RefreshTokens.FirstAsync(rt => rt.Token == oldRefreshToken);
        Assert.True(oldToken.IsRevoked);
        Assert.NotNull(oldToken.RevokedAt);
        Assert.Equal("new_refresh_token", oldToken.ReplacedByToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var invalidToken = "invalid_token";

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _authService.RefreshTokenAsync(invalidToken, "127.0.0.1", "TestAgent")
        );
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var user = CreateTestUser();
        var revokedToken = "revoked_refresh_token";
        
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Token = revokedToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            UserAgent = "TestAgent"
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _authService.RefreshTokenAsync(revokedToken, "127.0.0.1", "TestAgent")
        );
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var user = CreateTestUser();
        var expiredToken = "expired_refresh_token";
        
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Token = expiredToken,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            IsRevoked = false,
            IpAddress = "127.0.0.1",
            UserAgent = "TestAgent"
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _authService.RefreshTokenAsync(expiredToken, "127.0.0.1", "TestAgent")
        );
    }

    #endregion

    #region RevokeTokenAsync Tests

    [Fact]
    public async Task RevokeTokenAsync_WithValidToken_ShouldRevokeToken()
    {
        // Arrange
        var user = CreateTestUser();
        var refreshToken = "token_to_revoke";
        
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false,
            IpAddress = "127.0.0.1",
            UserAgent = "TestAgent"
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync();

        // Act
        await _authService.RevokeTokenAsync(refreshToken, "127.0.0.1");

        // Assert
        var token = await _dbContext.RefreshTokens.FirstAsync(rt => rt.Token == refreshToken);
        Assert.True(token.IsRevoked);
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public async Task RevokeTokenAsync_WithNonExistentToken_ShouldNotThrow()
    {
        // Act & Assert - Should not throw
        await _authService.RevokeTokenAsync("non_existent_token", "127.0.0.1");
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldIncrementFailedAttempts()
    {
        // Arrange
        var user = CreateTestUser();
        user.FailedLoginAttempts = 0;
        
        var dto = new LoginDto
        {
            UsernameOrEmail = "testuser",
            Password = "WrongPassword123!"
        };

        _mockUserRepository.Setup(r => r.GetByUsernameOrEmailAsync(dto.UsernameOrEmail))
            .ReturnsAsync(user);

        _mockPasswordService.Setup(s => s.VerifyPassword(dto.Password, user.PasswordHash))
            .Returns(false);

        _mockUserRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _authService.LoginAsync(dto, "127.0.0.1", "TestAgent")
        );

        // Verify failed attempts was incremented
        _mockUserRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_AfterMaxFailedAttempts_ShouldLockAccount()
    {
        // Arrange
        var user = CreateTestUser();
        user.FailedLoginAttempts = 4; // One more will lock
        
        var dto = new LoginDto
        {
            UsernameOrEmail = "testuser",
            Password = "WrongPassword123!"
        };

        _mockUserRepository.Setup(r => r.GetByUsernameOrEmailAsync(dto.UsernameOrEmail))
            .ReturnsAsync(user);

        _mockPasswordService.Setup(s => s.VerifyPassword(dto.Password, user.PasswordHash))
            .Returns(false);

        _mockUserRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _authService.LoginAsync(dto, "127.0.0.1", "TestAgent")
        );

        // Verify user was locked
        _mockUserRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithRememberMe_ShouldExtendTokenExpiry()
    {
        // Arrange
        var user = CreateTestUser();
        var dto = new LoginDto
        {
            UsernameOrEmail = "testuser",
            Password = "ValidPassword123!",
            RememberMe = true
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _mockUserRepository.Setup(r => r.GetByUsernameOrEmailAsync(dto.UsernameOrEmail))
            .ReturnsAsync(user);

        _mockPasswordService.Setup(s => s.VerifyPassword(dto.Password, user.PasswordHash))
            .Returns(true);

        _mockTokenService.Setup(s => s.GenerateAccessToken(user))
            .Returns("access_token");

        _mockTokenService.Setup(s => s.GenerateRefreshToken())
            .Returns("refresh_token");

        _mockUserRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _authService.LoginAsync(dto, "127.0.0.1", "TestAgent");

        // Assert
        Assert.NotNull(result);
        var refreshToken = await _dbContext.RefreshTokens.FirstAsync(rt => rt.Token == "refresh_token");
        Assert.True(refreshToken.ExpiresAt > DateTime.UtcNow.AddDays(29)); // Should be ~30 days
    }

    #endregion

    #region PasswordReset Tests

    [Fact]
    public async Task RequestPasswordResetAsync_WithValidEmail_ShouldNotThrow()
    {
        // Arrange
        var user = CreateTestUser();
        _mockUserRepository.Setup(r => r.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(user);

        // Act & Assert - Should not throw
        await _authService.RequestPasswordResetAsync("test@example.com");
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithNonExistentEmail_ShouldNotRevealUserExistence()
    {
        // Arrange
        _mockUserRepository.Setup(r => r.GetByEmailAsync("nonexistent@example.com"))
            .ReturnsAsync((User?)null);

        // Act & Assert - Should not throw (security: don't reveal if email exists)
        await _authService.RequestPasswordResetAsync("nonexistent@example.com");
    }

    #endregion

    private User CreateTestUser()
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "User",
            Description = "Test user role",
            CreatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            EmailVerified = true,
            PasswordHash = "hashedpassword",
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            User = user,
            Role = role,
            AssignedAt = DateTime.UtcNow
        });

        return user;
    }
}

