using System.ComponentModel.DataAnnotations;

namespace TemplateService.Core.DTOs;

/// <summary>
/// DTO for updating an existing template.
/// </summary>
public class UpdateTemplateDto
{
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string? Name { get; set; }

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    public string? TopologyJson { get; set; }

    public bool? IsPublic { get; set; }

    public List<string>? Tags { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string? Metadata { get; set; }
}

