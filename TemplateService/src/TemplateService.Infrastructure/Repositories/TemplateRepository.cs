using Microsoft.EntityFrameworkCore;
using TemplateService.Core.Interfaces;
using TemplateService.Core.Models;
using TemplateService.Infrastructure.Data;

namespace TemplateService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Template operations.
/// </summary>
public class TemplateRepository : ITemplateRepository
{
    private readonly TemplateDbContext _context;

    public TemplateRepository(TemplateDbContext context)
    {
        _context = context;
    }

    public async Task<Template?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Templates
            .Include(t => t.Tags)
            .ThenInclude(tt => tt.Tag)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<List<Template>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Templates
            .Include(t => t.Tags)
            .ThenInclude(tt => tt.Tag)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Template>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.Templates
            .Include(t => t.Tags)
            .ThenInclude(tt => tt.Tag)
            .Where(t => t.OwnerId == ownerId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Template>> GetPublicTemplatesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Templates
            .Include(t => t.Tags)
            .ThenInclude(tt => tt.Tag)
            .Where(t => t.IsPublic)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Template>> SearchAsync(string searchTerm, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var lowerSearchTerm = searchTerm.ToLower();

        return await _context.Templates
            .Include(t => t.Tags)
            .ThenInclude(tt => tt.Tag)
            .Where(t => t.Name.ToLower().Contains(lowerSearchTerm) || 
                       (t.Description != null && t.Description.ToLower().Contains(lowerSearchTerm)))
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Template>> GetByTagsAsync(List<string> tags, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var lowerTags = tags.Select(t => t.ToLower()).ToList();

        return await _context.Templates
            .Include(t => t.Tags)
            .ThenInclude(tt => tt.Tag)
            .Where(t => t.Tags.Any(tt => lowerTags.Contains(tt.Tag.Name.ToLower())))
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<Template> CreateAsync(Template template, CancellationToken cancellationToken = default)
    {
        _context.Templates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task<Template> UpdateAsync(Template template, CancellationToken cancellationToken = default)
    {
        template.UpdatedAt = DateTime.UtcNow;
        _context.Templates.Update(template);
        await _context.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await _context.Templates.FindAsync(new object[] { id }, cancellationToken);
        if (template != null)
        {
            _context.Templates.Remove(template);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Templates.AnyAsync(t => t.Id == id, cancellationToken);
    }

    public async Task IncrementUsageCountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await _context.Templates.FindAsync(new object[] { id }, cancellationToken);
        if (template != null)
        {
            template.UsageCount++;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

