using System.ComponentModel.DataAnnotations;

namespace TemplateService.Core.Models;

/// <summary>
/// Represents a network simulation template.
/// </summary>
public class Template
{
    /// <summary>
    /// Unique identifier for the template.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the template.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the template.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// ID of the user who created this template.
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Network topology configuration in JSON format.
    /// </summary>
    [Required]
    public string TopologyJson { get; set; } = string.Empty;

    /// <summary>
    /// Template version number.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Whether this template is publicly visible.
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Tags for categorization and search.
    /// </summary>
    public List<TemplateTag> Tags { get; set; } = new();

    /// <summary>
    /// Thumbnail image URL (optional).
    /// </summary>
    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Number of times this template has been used.
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// If this template was cloned from another, stores the original template ID.
    /// </summary>
    public Guid? ClonedFromId { get; set; }

    /// <summary>
    /// Reference to the original template if this is a clone.
    /// </summary>
    public Template? ClonedFrom { get; set; }

    /// <summary>
    /// When the template was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the template was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Metadata for additional configuration.
    /// </summary>
    public string? Metadata { get; set; }
}

