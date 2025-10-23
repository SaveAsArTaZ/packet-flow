using TemplateService.Core.DTOs;

namespace TemplateService.Core.Interfaces;

/// <summary>
/// Service interface for Template operations.
/// </summary>
public interface ITemplateService
{
    Task<TemplateDto?> GetByIdAsync(Guid id, Guid? requestingUserId = null, CancellationToken cancellationToken = default);
    Task<List<TemplateSummaryDto>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<TemplateSummaryDto>> GetMyTemplatesAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<TemplateSummaryDto>> GetPublicTemplatesAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<TemplateSummaryDto>> SearchAsync(string searchTerm, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<TemplateSummaryDto>> GetByTagsAsync(List<string> tags, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<TemplateDto> CreateAsync(CreateTemplateDto dto, Guid ownerId, CancellationToken cancellationToken = default);
    Task<TemplateDto> UpdateAsync(Guid id, UpdateTemplateDto dto, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<TemplateDto> CloneAsync(Guid id, Guid newOwnerId, string? newName = null, CancellationToken cancellationToken = default);
}

