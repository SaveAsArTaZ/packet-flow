using System.ComponentModel.DataAnnotations;

namespace TemplateService.Core.Models;

/// <summary>
/// Represents a tag for categorizing templates.
/// </summary>
public class TemplateTag
{
    /// <summary>
    /// Template ID.
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Reference to the template.
    /// </summary>
    public Template Template { get; set; } = null!;

    /// <summary>
    /// Tag ID.
    /// </summary>
    public Guid TagId { get; set; }

    /// <summary>
    /// Reference to the tag.
    /// </summary>
    public Tag Tag { get; set; } = null!;

    /// <summary>
    /// When the tag was added to the template.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

