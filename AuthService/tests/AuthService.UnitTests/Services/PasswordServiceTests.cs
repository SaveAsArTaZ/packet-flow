using AuthService.Infrastructure.Services;
using Xunit;

namespace AuthService.UnitTests.Services;

public class PasswordServiceTests
{
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _passwordService = new PasswordService();
    }

    [Fact]
    public void HashPassword_ShouldReturnValidHash()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash = _passwordService.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(wrongPassword, hash);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("", false, "Password is required")]
    [InlineData("   ", false, "Password is required")]
    [InlineData("short", false, "Password must be at least 8 characters")]
    [InlineData("alllowercase123!", false, "Password must contain at least one uppercase letter")]
    [InlineData("ALLUPPERCASE123!", false, "Password must contain at least one lowercase letter")]
    [InlineData("NoDigitsHere!", false, "Password must contain at least one number")]
    [InlineData("NoSpecialChar123", false, "Password must contain at least one special character")]
    [InlineData("ValidPassword123!", true, null)]
    public void ValidatePassword_ShouldReturnExpectedResult(string password, bool expectedIsValid, string? expectedError)
    {
        // Act
        var (isValid, error) = _passwordService.ValidatePassword(password);

        // Assert
        Assert.Equal(expectedIsValid, isValid);
        Assert.Equal(expectedError, error);
    }

    [Fact]
    public void HashPassword_ShouldProduceDifferentHashForSamePassword()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _passwordService.HashPassword(password);
        var hash2 = _passwordService.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2); // BCrypt uses salt, so hashes should differ
    }

    [Fact]
    public void VerifyPassword_WithInvalidHash_ShouldReturnFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var invalidHash = "invalid-hash";

        // Act
        var result = _passwordService.VerifyPassword(password, invalidHash);

        // Assert
        Assert.False(result);
    }
}


