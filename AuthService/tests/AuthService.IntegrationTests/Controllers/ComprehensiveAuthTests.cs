using AuthService.API.Controllers;
using AuthService.Core.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AuthService.IntegrationTests.Controllers;

public class ComprehensiveAuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ComprehensiveAuthTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithMismatchedPasswords_ShouldReturnBadRequest()
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

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var email = $"duplicate_{Guid.NewGuid():N}@example.com";
        
        var dto1 = new RegisterDto
        {
            Username = $"user1_{Guid.NewGuid():N}",
            Email = email,
            Password = "ValidPassword123!",
            ConfirmPassword = "ValidPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Register first user
        await _client.PostAsJsonAsync("/api/auth/register", dto1);

        var dto2 = new RegisterDto
        {
            Username = $"user2_{Guid.NewGuid():N}",
            Email = email, // Same email
            Password = "ValidPassword123!",
            ConfirmPassword = "ValidPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", dto2);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUsername_ShouldReturnOk()
    {
        // Arrange
        var username = $"testuser_{Guid.NewGuid():N}";
        var password = "ValidPassword123!";

        var registerDto = new RegisterDto
        {
            Username = username,
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = password,
            ConfirmPassword = password,
            FirstName = "Test",
            LastName = "User"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new LoginDto
        {
            UsernameOrEmail = username, // Using username instead of email
            Password = password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange - No token in cookies

        // Act
        var response = await _client.PostAsync("/api/auth/refresh", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid.token.here");

        // Act
        var response = await _client.GetAsync("/api/user/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var token = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var updateDto = new UpdateProfileDto(
            "UpdatedFirstName",
            "UpdatedLastName",
            "https://example.com/new-avatar.jpg"
        );

        // Act
        var response = await _client.PutAsJsonAsync("/api/user/profile", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify profile was updated
        var profileResponse = await _client.GetAsync("/api/user/me");
        var profile = await profileResponse.Content.ReadFromJsonAsync<UserInfoDto>();
        
        Assert.Equal("UpdatedFirstName", profile!.FirstName);
        Assert.Equal("UpdatedLastName", profile.LastName);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturnBadRequest()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "123", // Too weak
            ConfirmPassword = "123",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMultipleFailedAttempts_ShouldEventuallyLockAccount()
    {
        // Arrange
        var username = $"testuser_{Guid.NewGuid():N}";
        var correctPassword = "ValidPassword123!";

        var registerDto = new RegisterDto
        {
            Username = username,
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = correctPassword,
            ConfirmPassword = correctPassword,
            FirstName = "Test",
            LastName = "User"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Attempt login with wrong password multiple times
        for (int i = 0; i < 5; i++)
        {
            var loginDto = new LoginDto
            {
                UsernameOrEmail = username,
                Password = "WrongPassword123!"
            };

            await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        }

        // Try with correct password - should be locked
        var finalLoginDto = new LoginDto
        {
            UsernameOrEmail = username,
            Password = correctPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", finalLoginDto);

        // Assert - Account should be locked (returns Unauthorized, not Forbidden)
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> RegisterAndGetToken()
    {
        var dto = new RegisterDto
        {
            Username = $"testuser_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "ValidPassword123!",
            ConfirmPassword = "ValidPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);
        var result = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        
        return result!.AccessToken;
    }
}

