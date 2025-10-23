using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TemplateService.Core.DTOs;
using Xunit;

namespace TemplateService.IntegrationTests;

public class ComprehensiveTemplateApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ComprehensiveTemplateApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetPublicTemplates_ReturnsSuccessAndListOfTemplates()
    {
        // Act
        var response = await _client.GetAsync("/api/templates/public?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var templates = await response.Content.ReadFromJsonAsync<List<TemplateSummaryDto>>();
        Assert.NotNull(templates);
    }

    [Fact]
    public async Task GetTemplateById_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var randomId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/templates/{randomId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateTemplate_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var createDto = new CreateTemplateDto
        {
            Name = "Test Template",
            Description = "Test Description",
            TopologyJson = "{}",
            IsPublic = true,
            Tags = new List<string> { "wifi" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/templates", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SearchTemplates_WithQuery_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/templates/search?q=test&page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var templates = await response.Content.ReadFromJsonAsync<List<TemplateSummaryDto>>();
        Assert.NotNull(templates);
    }

    [Fact]
    public async Task SearchTemplates_WithoutQuery_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/templates/search?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTemplatesByTags_WithValidTags_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/templates/tags?tags=wifi&tags=p2p&page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var templates = await response.Content.ReadFromJsonAsync<List<TemplateSummaryDto>>();
        Assert.NotNull(templates);
    }

    [Fact]
    public async Task GetTemplatesByTags_WithoutTags_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/templates/tags?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTemplate_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var updateDto = new UpdateTemplateDto
        {
            Name = "Updated Name"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/templates/{templateId}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTemplate_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var templateId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/templates/{templateId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CloneTemplate_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var templateId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/templates/{templateId}/clone", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyTemplates_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/templates/my?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllTemplates_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/templates?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

