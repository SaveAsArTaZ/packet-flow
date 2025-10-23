using TemplateService.Core.Models;

namespace TemplateService.Core.Interfaces;

/// <summary>
/// Repository interface for Template operations.
/// </summary>
public interface ITemplateRepository
{
    Task<Template?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Template>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<Template>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<List<Template>> GetPublicTemplatesAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<Template>> SearchAsync(string searchTerm, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<Template>> GetByTagsAsync(List<string> tags, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Template> CreateAsync(Template template, CancellationToken cancellationToken = default);
    Task<Template> UpdateAsync(Template template, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task IncrementUsageCountAsync(Guid id, CancellationToken cancellationToken = default);
}

