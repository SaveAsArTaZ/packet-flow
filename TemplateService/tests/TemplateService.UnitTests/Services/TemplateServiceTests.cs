using Moq;
using TemplateService.Core.Interfaces;
using TemplateService.Core.Models;
using Xunit;
using InfraTemplateService = TemplateService.Infrastructure.Services.TemplateService;

namespace TemplateService.UnitTests.Services;

public class TemplateServiceTests
{
    [Fact]
    public async Task GetByIdAsync_ExistingTemplate_ReturnsTemplate()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var template = new Template
        {
            Id = templateId,
            Name = "Test Template",
            OwnerId = ownerId,
            TopologyJson = "{}",
            IsPublic = true
        };

        var mockTemplateRepo = new Mock<ITemplateRepository>();
        mockTemplateRepo.Setup(r => r.GetByIdAsync(templateId, default))
            .ReturnsAsync(template);

        var mockTagRepo = new Mock<ITagRepository>();
        var service = new InfraTemplateService(mockTemplateRepo.Object, mockTagRepo.Object);

        // Act
        var result = await service.GetByIdAsync(templateId, ownerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(templateId, result.Id);
        Assert.Equal("Test Template", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingTemplate_ReturnsNull()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var mockTemplateRepo = new Mock<ITemplateRepository>();
        mockTemplateRepo.Setup(r => r.GetByIdAsync(templateId, default))
            .ReturnsAsync((Template?)null);

        var mockTagRepo = new Mock<ITagRepository>();
        var service = new InfraTemplateService(mockTemplateRepo.Object, mockTagRepo.Object);

        // Act
        var result = await service.GetByIdAsync(templateId);

        // Assert
        Assert.Null(result);
    }
}

