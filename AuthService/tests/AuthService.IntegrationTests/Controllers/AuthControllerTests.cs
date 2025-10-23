using AuthService.Core.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AuthService.IntegrationTests.Controllers;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = $"testuser_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "ValidPassword123!",
            ConfirmPassword = "ValidPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        Assert.NotNull(result);
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        Assert.NotNull(result.User);
        Assert.Equal(dto.Email, result.User.Email);
    }

    [Fact]
    public async Task Register_WithInvalidPassword_ShouldReturnBadRequest()
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

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOk()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = $"testuser_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "ValidPassword123!",
            ConfirmPassword = "ValidPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Register first
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new LoginDto
        {
            UsernameOrEmail = registerDto.Email,
            Password = registerDto.Password,
            RememberMe = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        Assert.NotNull(result);
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            UsernameOrEmail = "nonexistent@example.com",
            Password = "WrongPassword123!",
            RememberMe = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldReturnOk()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = $"testuser_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "ValidPassword123!",
            ConfirmPassword = "ValidPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Register to get a refresh token
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<TokenResponseDto>();

        // Extract cookies from the registration response
        var cookies = registerResponse.Headers.GetValues("Set-Cookie").FirstOrDefault();
        if (cookies != null)
        {
            _client.DefaultRequestHeaders.Add("Cookie", cookies);
        }

        // Act
        var refreshResponse = await _client.PostAsync("/api/auth/refresh", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_WhenAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = $"testuser_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "ValidPassword123!",
            ConfirmPassword = "ValidPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Register and login
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<TokenResponseDto>();

        // Add auth header
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registerResult!.AccessToken);

        // Act
        var logoutResponse = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
    }
}

