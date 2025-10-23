using AuthService.Core.DTOs;
using AuthService.API.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AuthService.IntegrationTests.Controllers;

public class UserControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UserControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProfile_WhenAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var token = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/user/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<UserInfoDto>();
        Assert.NotNull(result);
        Assert.NotNull(result.Email);
        Assert.NotNull(result.Username);
    }

    [Fact]
    public async Task GetProfile_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/user/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WhenAuthenticated_ShouldReturnOk()
    {
        // Arrange
        var token = await RegisterAndGetToken();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var updateDto = new UpdateProfileDto(
            "Updated",
            "Name",
            "https://example.com/avatar.jpg"
        );

        // Act
        var response = await _client.PutAsJsonAsync("/api/user/profile", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var updateDto = new UpdateProfileDto(
            "Updated",
            "Name",
            "https://example.com/avatar.jpg"
        );

        // Act
        var response = await _client.PutAsJsonAsync("/api/user/profile", updateDto);

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

