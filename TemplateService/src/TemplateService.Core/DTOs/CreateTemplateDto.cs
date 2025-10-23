using System.ComponentModel.DataAnnotations;

namespace TemplateService.Core.DTOs;

/// <summary>
/// DTO for creating a new template.
/// </summary>
public class CreateTemplateDto
{
    [Required(ErrorMessage = "Template name is required")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Topology JSON is required")]
    public string TopologyJson { get; set; } = string.Empty;

    public bool IsPublic { get; set; }

    public List<string> Tags { get; set; } = new();

    public string? ThumbnailUrl { get; set; }

    public string? Metadata { get; set; }
}

