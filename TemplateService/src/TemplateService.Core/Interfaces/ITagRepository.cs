using TemplateService.Core.Models;

namespace TemplateService.Core.Interfaces;

/// <summary>
/// Repository interface for Tag operations.
/// </summary>
public interface ITagRepository
{
    Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<Tag>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Tag>> GetOrCreateAsync(List<string> tagNames, CancellationToken cancellationToken = default);
    Task<Tag> CreateAsync(Tag tag, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

