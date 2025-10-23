using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace TemplateService.IntegrationTests;

public class TemplateApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TemplateApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPublicTemplates_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/templates/public");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTemplateById_WithoutAuth_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var randomId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/templates/{randomId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

