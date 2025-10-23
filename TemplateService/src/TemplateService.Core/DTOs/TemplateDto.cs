namespace TemplateService.Core.DTOs;

/// <summary>
/// DTO for template information.
/// </summary>
public class TemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public string TopologyJson { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool IsPublic { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? ThumbnailUrl { get; set; }
    public int UsageCount { get; set; }
    public Guid? ClonedFromId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Metadata { get; set; }
}

