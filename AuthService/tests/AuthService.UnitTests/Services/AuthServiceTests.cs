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

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IPasswordService> _mockPasswordService;
    private readonly Mock<ILogger<Infrastructure.Services.AuthService>> _mockLogger;
    private readonly AuthDbContext _dbContext;
    private readonly Infrastructure.Services.AuthService _authService;

    public AuthServiceTests()
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

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "ValidPassword123!",
            ConfirmPassword = "ValidPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        _mockPasswordService.Setup(s => s.ValidatePassword(dto.Password))
            .Returns((true, null));
        
        _mockPasswordService.Setup(s => s.HashPassword(dto.Password))
            .Returns("hashedpassword");

        _mockUserRepository.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(false);

        _mockUserRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockTokenService.Setup(s => s.GenerateAccessToken(It.IsAny<User>()))
            .Returns("access_token");

        _mockTokenService.Setup(s => s.GenerateRefreshToken())
            .Returns("refresh_token");

        // Act
        var result = await _authService.RegisterAsync(dto, "127.0.0.1", "TestAgent");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("access_token", result.AccessToken);
        Assert.Equal("refresh_token", result.RefreshToken);
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidPassword_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "weak",
            ConfirmPassword = "weak",
            FirstName = "Test",
            LastName = "User"
        };

        _mockPasswordService.Setup(s => s.ValidatePassword(dto.Password))
            .Returns((false, "Password is too weak"));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _authService.RegisterAsync(dto, "127.0.0.1", "TestAgent")
        );
    }

    [Fact]
    public async Task RegisterAsync_WithMismatchedPasswords_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "ValidPassword123!",
            ConfirmPassword = "DifferentPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        _mockPasswordService.Setup(s => s.ValidatePassword(dto.Password))
            .Returns((true, null));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _authService.RegisterAsync(dto, "127.0.0.1", "TestAgent")
        );

        Assert.Equal("Passwords do not match", exception.Message);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "testuser",
            Email = "existing@example.com",
            Password = "ValidPassword123!",
            ConfirmPassword = "ValidPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        _mockPasswordService.Setup(s => s.ValidatePassword(dto.Password))
            .Returns((true, null));

        _mockUserRepository.Setup(r => r.ExistsAsync(It.Is<System.Linq.Expressions.Expression<Func<User, bool>>>(
            expr => true))) // Email check
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _authService.RegisterAsync(dto, "127.0.0.1", "TestAgent")
        );

        Assert.Equal("Email already registered", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var user = CreateTestUser();
        var dto = new LoginDto
        {
            UsernameOrEmail = "testuser",
            Password = "ValidPassword123!",
            RememberMe = false
        };

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
        Assert.Equal("access_token", result.AccessToken);
        Assert.Equal("refresh_token", result.RefreshToken);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var dto = new LoginDto
        {
            UsernameOrEmail = "nonexistent",
            Password = "password"
        };

        _mockUserRepository.Setup(r => r.GetByUsernameOrEmailAsync(dto.UsernameOrEmail))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _authService.LoginAsync(dto, "127.0.0.1", "TestAgent")
        );
    }

    [Fact]
    public async Task LoginAsync_WithLockedAccount_ShouldThrowAccountLockedException()
    {
        // Arrange
        var user = CreateTestUser();
        user.IsLocked = true;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);

        var dto = new LoginDto
        {
            UsernameOrEmail = "testuser",
            Password = "password"
        };

        _mockUserRepository.Setup(r => r.GetByUsernameOrEmailAsync(dto.UsernameOrEmail))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<AccountLockedException>(
            () => _authService.LoginAsync(dto, "127.0.0.1", "TestAgent")
        );
    }

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

