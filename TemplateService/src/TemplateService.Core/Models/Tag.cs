using System.ComponentModel.DataAnnotations;

namespace TemplateService.Core.Models;

/// <summary>
/// Represents a tag that can be applied to templates.
/// </summary>
public class Tag
{
    /// <summary>
    /// Unique identifier for the tag.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tag name (e.g., "wifi", "p2p", "csma").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tag description.
    /// </summary>
    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// Color code for UI display.
    /// </summary>
    [MaxLength(7)]
    public string? ColorCode { get; set; }

    /// <summary>
    /// Templates associated with this tag.
    /// </summary>
    public List<TemplateTag> TemplateTags { get; set; } = new();

    /// <summary>
    /// When the tag was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

