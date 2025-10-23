using TemplateService.Core.DTOs;
using TemplateService.Core.Exceptions;
using TemplateService.Core.Interfaces;
using TemplateService.Core.Models;

namespace TemplateService.Infrastructure.Services;

/// <summary>
/// Service implementation for Template operations.
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly ITemplateRepository _templateRepository;
    private readonly ITagRepository _tagRepository;

    public TemplateService(ITemplateRepository templateRepository, ITagRepository tagRepository)
    {
        _templateRepository = templateRepository;
        _tagRepository = tagRepository;
    }

    public async Task<TemplateDto?> GetByIdAsync(Guid id, Guid? requestingUserId = null, CancellationToken cancellationToken = default)
    {
        var template = await _templateRepository.GetByIdAsync(id, cancellationToken);
        
        if (template == null)
            return null;

        // Check if the template is accessible
        if (!template.IsPublic && template.OwnerId != requestingUserId)
            throw new ForbiddenAccessException("You do not have permission to access this template.");

        return MapToDto(template);
    }

    public async Task<List<TemplateSummaryDto>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var templates = await _templateRepository.GetAllAsync(page, pageSize, cancellationToken);
        return templates.Select(MapToSummaryDto).ToList();
    }

    public async Task<List<TemplateSummaryDto>> GetMyTemplatesAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var templates = await _templateRepository.GetByOwnerIdAsync(userId, cancellationToken);
        return templates.Skip((page - 1) * pageSize).Take(pageSize).Select(MapToSummaryDto).ToList();
    }

    public async Task<List<TemplateSummaryDto>> GetPublicTemplatesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var templates = await _templateRepository.GetPublicTemplatesAsync(page, pageSize, cancellationToken);
        return templates.Select(MapToSummaryDto).ToList();
    }

    public async Task<List<TemplateSummaryDto>> SearchAsync(string searchTerm, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var templates = await _templateRepository.SearchAsync(searchTerm, page, pageSize, cancellationToken);
        return templates.Select(MapToSummaryDto).ToList();
    }

    public async Task<List<TemplateSummaryDto>> GetByTagsAsync(List<string> tags, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var templates = await _templateRepository.GetByTagsAsync(tags, page, pageSize, cancellationToken);
        return templates.Select(MapToSummaryDto).ToList();
    }

    public async Task<TemplateDto> CreateAsync(CreateTemplateDto dto, Guid ownerId, CancellationToken cancellationToken = default)
    {
        // Validate topology JSON
        ValidateTopologyJson(dto.TopologyJson);

        // Get or create tags
        var tags = await _tagRepository.GetOrCreateAsync(dto.Tags, cancellationToken);

        var template = new Template
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            OwnerId = ownerId,
            TopologyJson = dto.TopologyJson,
            IsPublic = dto.IsPublic,
            ThumbnailUrl = dto.ThumbnailUrl,
            Metadata = dto.Metadata,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = tags.Select(tag => new TemplateTag
            {
                TagId = tag.Id,
                Tag = tag,
                CreatedAt = DateTime.UtcNow
            }).ToList()
        };

        var createdTemplate = await _templateRepository.CreateAsync(template, cancellationToken);
        return MapToDto(createdTemplate);
    }

    public async Task<TemplateDto> UpdateAsync(Guid id, UpdateTemplateDto dto, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        var template = await _templateRepository.GetByIdAsync(id, cancellationToken);
        
        if (template == null)
            throw new NotFoundException("Template", id);

        if (template.OwnerId != requestingUserId)
            throw new ForbiddenAccessException("You do not have permission to update this template.");

        // Update fields
        if (dto.Name != null)
            template.Name = dto.Name;

        if (dto.Description != null)
            template.Description = dto.Description;

        if (dto.TopologyJson != null)
        {
            ValidateTopologyJson(dto.TopologyJson);
            template.TopologyJson = dto.TopologyJson;
            template.Version++;
        }

        if (dto.IsPublic.HasValue)
            template.IsPublic = dto.IsPublic.Value;

        if (dto.ThumbnailUrl != null)
            template.ThumbnailUrl = dto.ThumbnailUrl;

        if (dto.Metadata != null)
            template.Metadata = dto.Metadata;

        if (dto.Tags != null)
        {
            // Update tags
            var newTags = await _tagRepository.GetOrCreateAsync(dto.Tags, cancellationToken);
            template.Tags = newTags.Select(tag => new TemplateTag
            {
                TemplateId = template.Id,
                TagId = tag.Id,
                Tag = tag,
                CreatedAt = DateTime.UtcNow
            }).ToList();
        }

        var updatedTemplate = await _templateRepository.UpdateAsync(template, cancellationToken);
        return MapToDto(updatedTemplate);
    }

    public async Task DeleteAsync(Guid id, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        var template = await _templateRepository.GetByIdAsync(id, cancellationToken);
        
        if (template == null)
            throw new NotFoundException("Template", id);

        if (template.OwnerId != requestingUserId)
            throw new ForbiddenAccessException("You do not have permission to delete this template.");

        await _templateRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<TemplateDto> CloneAsync(Guid id, Guid newOwnerId, string? newName = null, CancellationToken cancellationToken = default)
    {
        var originalTemplate = await _templateRepository.GetByIdAsync(id, cancellationToken);
        
        if (originalTemplate == null)
            throw new NotFoundException("Template", id);

        if (!originalTemplate.IsPublic && originalTemplate.OwnerId != newOwnerId)
            throw new ForbiddenAccessException("You do not have permission to clone this template.");

        // Increment usage count of original
        await _templateRepository.IncrementUsageCountAsync(id, cancellationToken);

        var clonedTemplate = new Template
        {
            Id = Guid.NewGuid(),
            Name = newName ?? $"{originalTemplate.Name} (Copy)",
            Description = originalTemplate.Description,
            OwnerId = newOwnerId,
            TopologyJson = originalTemplate.TopologyJson,
            IsPublic = false, // Clones are private by default
            ThumbnailUrl = originalTemplate.ThumbnailUrl,
            Metadata = originalTemplate.Metadata,
            ClonedFromId = originalTemplate.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = originalTemplate.Tags.Select(tt => new TemplateTag
            {
                TagId = tt.TagId,
                Tag = tt.Tag,
                CreatedAt = DateTime.UtcNow
            }).ToList()
        };

        var createdTemplate = await _templateRepository.CreateAsync(clonedTemplate, cancellationToken);
        return MapToDto(createdTemplate);
    }

    private TemplateDto MapToDto(Template template)
    {
        return new TemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            OwnerId = template.OwnerId,
            TopologyJson = template.TopologyJson,
            Version = template.Version,
            IsPublic = template.IsPublic,
            Tags = template.Tags.Select(tt => tt.Tag.Name).ToList(),
            ThumbnailUrl = template.ThumbnailUrl,
            UsageCount = template.UsageCount,
            ClonedFromId = template.ClonedFromId,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            Metadata = template.Metadata
        };
    }

    private TemplateSummaryDto MapToSummaryDto(Template template)
    {
        return new TemplateSummaryDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            OwnerId = template.OwnerId,
            IsPublic = template.IsPublic,
            Tags = template.Tags.Select(tt => tt.Tag.Name).ToList(),
            ThumbnailUrl = template.ThumbnailUrl,
            UsageCount = template.UsageCount,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }

    private void ValidateTopologyJson(string json)
    {
        // Basic validation - ensure it's valid JSON
        try
        {
            System.Text.Json.JsonDocument.Parse(json);
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new ValidationException("Invalid topology JSON format.", ex);
        }
    }
}

