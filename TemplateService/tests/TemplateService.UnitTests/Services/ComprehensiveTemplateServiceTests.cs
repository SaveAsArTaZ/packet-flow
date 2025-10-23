using Moq;
using TemplateService.Core.DTOs;
using TemplateService.Core.Exceptions;
using TemplateService.Core.Interfaces;
using TemplateService.Core.Models;
using Xunit;
using InfraTemplateService = TemplateService.Infrastructure.Services.TemplateService;

namespace TemplateService.UnitTests.Services;

public class ComprehensiveTemplateServiceTests
{
    private readonly Mock<ITemplateRepository> _mockTemplateRepo;
    private readonly Mock<ITagRepository> _mockTagRepo;
    private readonly InfraTemplateService _service;

    public ComprehensiveTemplateServiceTests()
    {
        _mockTemplateRepo = new Mock<ITemplateRepository>();
        _mockTagRepo = new Mock<ITagRepository>();
        _service = new InfraTemplateService(_mockTemplateRepo.Object, _mockTagRepo.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_PrivateTemplate_OwnerAccess_ReturnsTemplate()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var template = CreateTestTemplate(templateId, ownerId, isPublic: false);

        _mockTemplateRepo.Setup(r => r.GetByIdAsync(templateId, default))
            .ReturnsAsync(template);

        // Act
        var result = await _service.GetByIdAsync(templateId, ownerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(templateId, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_PrivateTemplate_NonOwnerAccess_ThrowsForbiddenException()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var template = CreateTestTemplate(templateId, ownerId, isPublic: false);

        _mockTemplateRepo.Setup(r => r.GetByIdAsync(templateId, default))
            .ReturnsAsync(template);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _service.GetByIdAsync(templateId, differentUserId));
    }

    [Fact]
    public async Task GetByIdAsync_PublicTemplate_AnyUserAccess_ReturnsTemplate()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var anyUserId = Guid.NewGuid();
        var template = CreateTestTemplate(templateId, ownerId, isPublic: true);

        _mockTemplateRepo.Setup(r => r.GetByIdAsync(templateId, default))
            .ReturnsAsync(template);

        // Act
        var result = await _service.GetByIdAsync(templateId, anyUserId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsPublic);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesTemplate()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var dto = new CreateTemplateDto
        {
            Name = "Test Template",
            Description = "Test Description",
            TopologyJson = "{}",
            IsPublic = true,
            Tags = new List<string> { "wifi", "p2p" }
        };

        var tags = new List<Tag>
        {
            new Tag { Id = Guid.NewGuid(), Name = "wifi" },
            new Tag { Id = Guid.NewGuid(), Name = "p2p" }
        };

        _mockTagRepo.Setup(r => r.GetOrCreateAsync(dto.Tags, default))
            .ReturnsAsync(tags);

        _mockTemplateRepo.Setup(r => r.CreateAsync(It.IsAny<Template>(), default))
            .ReturnsAsync((Template t, CancellationToken ct) => t);

        // Act
        var result = await _service.CreateAsync(dto, ownerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.Description, result.Description);
        Assert.Equal(ownerId, result.OwnerId);
        Assert.Equal(2, result.Tags.Count);
    }

    [Fact]
    public async Task CreateAsync_InvalidJson_ThrowsValidationException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var dto = new CreateTemplateDto
        {
            Name = "Test",
            TopologyJson = "invalid json {",
            Tags = new List<string>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(dto, ownerId));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidUpdate_UpdatesTemplate()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var template = CreateTestTemplate(templateId, ownerId, isPublic: false);
        template.Version = 1;

        var updateDto = new UpdateTemplateDto
        {
            Name = "Updated Name",
            Description = "Updated Description",
            TopologyJson = "{\"updated\": true}"
        };

        _mockTemplateRepo.Setup(r => r.GetByIdAsync(templateId, default))
            .ReturnsAsync(template);

        _mockTemplateRepo.Setup(r => r.UpdateAsync(It.IsAny<Template>(), default))
            .ReturnsAsync((Template t, CancellationToken ct) => t);

        // Act
        var result = await _service.UpdateAsync(templateId, updateDto, ownerId);

        // Assert
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("Updated Description", result.Description);
        Assert.Equal(2, result.Version); // Version should increment
    }

    [Fact]
    public async Task UpdateAsync_NonExistentTemplate_ThrowsNotFoundException()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var updateDto = new UpdateTemplateDto { Name = "Test" };

        _mockTemplateRepo.Setup(r => r.GetByIdAsync(templateId, default))
            .ReturnsAsync((Template?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.UpdateAsync(templateId, updateDto, ownerId));
    }

    [Fact]
    public async Task UpdateAsync_NonOwner_ThrowsForbiddenException()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var template = CreateTestTemplate(templateId, ownerId, isPublic: false);
        var updateDto = new UpdateTemplateDto { Name = "Test" };

        _mockTemplateRepo.Setup(r => r.GetByIdAsync(templateId, default))
            .ReturnsAsync(template);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _service.UpdateAsync(templateId, updateDto, differentUserId));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_OwnerDeletes_Succeeds()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var template = CreateTestTemplate(templateId, ownerId, isPublic: false);

        _mockTemplateRepo.Setup(r => r.GetByIdAsync(templateId, default))
            .ReturnsAsync(template);

        _mockTemplateRepo.Setup(r => r.DeleteAsync(templateId, default))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(templateId, ownerId);

        // Assert
        _mockTemplateRepo.Verify(r => r.DeleteAsync(templateId, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonOwner_ThrowsForbiddenException()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var template = CreateTestTemplate(templateId, ownerId, isPublic: false);

        _mockTemplateRepo.Setup(r => r.GetByIdAsync(templateId, default))
            .ReturnsAsync(template);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _service.DeleteAsync(templateId, differentUserId));
    }

    [Fact]
    public async Task DeleteAsync_NonExistentTemplate_ThrowsNotFoundException()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        _mockTemplateRepo.Setup(r => r.GetByIdAsync(templateId, default))
            .ReturnsAsync((Template?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.DeleteAsync(templateId, ownerId));
    }

    #endregion

    #region CloneAsync Tests

    [Fact]
    public async Task CloneAsync_PublicTemplate_Succeeds()
    {
        // Arrange
        var originalId = Guid.NewGuid();
        var originalOwnerId = Guid.NewGuid();
        var newOwnerId = Guid.NewGuid();
        var originalTemplate = CreateTestTemplate(originalId, originalOwnerId, isPublic: true);

        _mockTemplateRepo.Setup(r => r.GetByIdAsync(originalId, default))
            .ReturnsAsync(originalTemplate);

        _mockTemplateRepo.Setup(r => r.IncrementUsageCountAsync(originalId, default))
            .Returns(Task.CompletedTask);

        _mockTemplateRepo.Setup(r => r.CreateAsync(It.IsAny<Template>(), default))
            .ReturnsAsync((Template t, CancellationToken ct) => t);

        // Act
        var result = await _service.CloneAsync(originalId, newOwnerId);

        // Assert
        Assert.NotEqual(originalId, result.Id);
        Assert.Equal(newOwnerId, result.OwnerId);
        Assert.Equal(originalId, result.ClonedFromId);
        Assert.False(result.IsPublic); // Clones are private by default
        _mockTemplateRepo.Verify(r => r.IncrementUsageCountAsync(originalId, default), Times.Once);
    }

    [Fact]
    public async Task CloneAsync_WithCustomName_UsesCustomName()
    {
        // Arrange
        var originalId = Guid.NewGuid();
        var originalOwnerId = Guid.NewGuid();
        var newOwnerId = Guid.NewGuid();
        var customName = "My Cloned Template";
        var originalTemplate = CreateTestTemplate(originalId, originalOwnerId, isPublic: true);

        _mockTemplateRepo.Setup(r => r.GetByIdAsync(originalId, default))
            .ReturnsAsync(originalTemplate);

        _mockTemplateRepo.Setup(r => r.IncrementUsageCountAsync(originalId, default))
            .Returns(Task.CompletedTask);

        _mockTemplateRepo.Setup(r => r.CreateAsync(It.IsAny<Template>(), default))
            .ReturnsAsync((Template t, CancellationToken ct) => t);

        // Act
        var result = await _service.CloneAsync(originalId, newOwnerId, customName);

        // Assert
        Assert.Equal(customName, result.Name);
    }

    [Fact]
    public async Task CloneAsync_PrivateTemplateNonOwner_ThrowsForbiddenException()
    {
        // Arrange
        var originalId = Guid.NewGuid();
        var originalOwnerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var originalTemplate = CreateTestTemplate(originalId, originalOwnerId, isPublic: false);

        _mockTemplateRepo.Setup(r => r.GetByIdAsync(originalId, default))
            .ReturnsAsync(originalTemplate);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _service.CloneAsync(originalId, differentUserId));
    }

    [Fact]
    public async Task CloneAsync_NonExistentTemplate_ThrowsNotFoundException()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockTemplateRepo.Setup(r => r.GetByIdAsync(templateId, default))
            .ReturnsAsync((Template?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CloneAsync(templateId, userId));
    }

    #endregion

    #region Search and Filter Tests

    [Fact]
    public async Task GetAllAsync_ReturnsTemplates()
    {
        // Arrange
        var templates = new List<Template>
        {
            CreateTestTemplate(Guid.NewGuid(), Guid.NewGuid(), isPublic: true),
            CreateTestTemplate(Guid.NewGuid(), Guid.NewGuid(), isPublic: false)
        };

        _mockTemplateRepo.Setup(r => r.GetAllAsync(1, 20, default))
            .ReturnsAsync(templates);

        // Act
        var result = await _service.GetAllAsync(1, 20);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetPublicTemplatesAsync_ReturnsOnlyPublicTemplates()
    {
        // Arrange
        var templates = new List<Template>
        {
            CreateTestTemplate(Guid.NewGuid(), Guid.NewGuid(), isPublic: true),
            CreateTestTemplate(Guid.NewGuid(), Guid.NewGuid(), isPublic: true)
        };

        _mockTemplateRepo.Setup(r => r.GetPublicTemplatesAsync(1, 20, default))
            .ReturnsAsync(templates);

        // Act
        var result = await _service.GetPublicTemplatesAsync(1, 20);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.True(t.IsPublic));
    }

    [Fact]
    public async Task GetMyTemplatesAsync_ReturnsUserTemplates()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var templates = new List<Template>
        {
            CreateTestTemplate(Guid.NewGuid(), userId, isPublic: true),
            CreateTestTemplate(Guid.NewGuid(), userId, isPublic: false)
        };

        _mockTemplateRepo.Setup(r => r.GetByOwnerIdAsync(userId, default))
            .ReturnsAsync(templates);

        // Act
        var result = await _service.GetMyTemplatesAsync(userId, 1, 20);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal(userId, t.OwnerId));
    }

    [Fact]
    public async Task SearchAsync_ReturnsMatchingTemplates()
    {
        // Arrange
        var searchTerm = "wifi";
        var templates = new List<Template>
        {
            CreateTestTemplate(Guid.NewGuid(), Guid.NewGuid(), name: "WiFi Network"),
            CreateTestTemplate(Guid.NewGuid(), Guid.NewGuid(), name: "Advanced WiFi Setup")
        };

        _mockTemplateRepo.Setup(r => r.SearchAsync(searchTerm, 1, 20, default))
            .ReturnsAsync(templates);

        // Act
        var result = await _service.SearchAsync(searchTerm, 1, 20);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByTagsAsync_ReturnsTemplatesWithTags()
    {
        // Arrange
        var tags = new List<string> { "wifi", "p2p" };
        var templates = new List<Template>
        {
            CreateTestTemplate(Guid.NewGuid(), Guid.NewGuid(), isPublic: true)
        };

        _mockTemplateRepo.Setup(r => r.GetByTagsAsync(tags, 1, 20, default))
            .ReturnsAsync(templates);

        // Act
        var result = await _service.GetByTagsAsync(tags, 1, 20);

        // Assert
        Assert.Single(result);
    }

    #endregion

    // Helper method to create test templates
    private Template CreateTestTemplate(
        Guid id,
        Guid ownerId,
        bool isPublic = true,
        string name = "Test Template")
    {
        return new Template
        {
            Id = id,
            Name = name,
            Description = "Test Description",
            OwnerId = ownerId,
            TopologyJson = "{}",
            IsPublic = isPublic,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = new List<TemplateTag>()
        };
    }
}

