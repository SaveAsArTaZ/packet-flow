using Microsoft.EntityFrameworkCore;
using TemplateService.Core.Interfaces;
using TemplateService.Core.Models;
using TemplateService.Infrastructure.Data;

namespace TemplateService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Tag operations.
/// </summary>
public class TagRepository : ITagRepository
{
    private readonly TemplateDbContext _context;

    public TagRepository(TemplateDbContext context)
    {
        _context = context;
    }

    public async Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tags.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower(), cancellationToken);
    }

    public async Task<List<Tag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Tag>> GetOrCreateAsync(List<string> tagNames, CancellationToken cancellationToken = default)
    {
        var tags = new List<Tag>();

        foreach (var tagName in tagNames)
        {
            var existingTag = await GetByNameAsync(tagName, cancellationToken);
            if (existingTag != null)
            {
                tags.Add(existingTag);
            }
            else
            {
                var newTag = new Tag
                {
                    Id = Guid.NewGuid(),
                    Name = tagName.ToLower(),
                    CreatedAt = DateTime.UtcNow
                };
                tags.Add(await CreateAsync(newTag, cancellationToken));
            }
        }

        return tags;
    }

    public async Task<Tag> CreateAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync(cancellationToken);
        return tag;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tag = await _context.Tags.FindAsync(new object[] { id }, cancellationToken);
        if (tag != null)
        {
            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

