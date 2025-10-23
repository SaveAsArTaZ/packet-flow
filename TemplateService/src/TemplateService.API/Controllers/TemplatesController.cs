using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TemplateService.Core.DTOs;
using TemplateService.Core.Exceptions;
using TemplateService.Core.Interfaces;

namespace TemplateService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateService _templateService;
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(ITemplateService templateService, ILogger<TemplatesController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// Get template by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TemplateDto>> GetById(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var template = await _templateService.GetByIdAsync(id, userId);

            if (template == null)
                return NotFound(new { error = "Template not found" });

            return Ok(template);
        }
        catch (ForbiddenAccessException ex)
        {
            _logger.LogWarning("Forbidden access attempt: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all templates (paginated).
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(List<TemplateSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TemplateSummaryDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var templates = await _templateService.GetAllAsync(page, pageSize);
        return Ok(templates);
    }

    /// <summary>
    /// Get my templates.
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(typeof(List<TemplateSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TemplateSummaryDto>>> GetMyTemplates([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token" });

        var templates = await _templateService.GetMyTemplatesAsync(userId.Value, page, pageSize);
        return Ok(templates);
    }

    /// <summary>
    /// Get public templates.
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<TemplateSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TemplateSummaryDto>>> GetPublicTemplates([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var templates = await _templateService.GetPublicTemplatesAsync(page, pageSize);
        return Ok(templates);
    }

    /// <summary>
    /// Search templates by name or description.
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<TemplateSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TemplateSummaryDto>>> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Search query is required" });

        var templates = await _templateService.SearchAsync(q, page, pageSize);
        return Ok(templates);
    }

    /// <summary>
    /// Get templates by tags.
    /// </summary>
    [HttpGet("tags")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<TemplateSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TemplateSummaryDto>>> GetByTags([FromQuery] List<string> tags, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (tags == null || tags.Count == 0)
            return BadRequest(new { error = "At least one tag is required" });

        var templates = await _templateService.GetByTagsAsync(tags, page, pageSize);
        return Ok(templates);
    }

    /// <summary>
    /// Create a new template.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(TemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TemplateDto>> Create([FromBody] CreateTemplateDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { error = "User ID not found in token" });

            var template = await _templateService.CreateAsync(dto, userId.Value);
            return CreatedAtAction(nameof(GetById), new { id = template.Id }, template);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message, errors = ex.Errors });
        }
    }

    /// <summary>
    /// Update an existing template.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(TemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TemplateDto>> Update(Guid id, [FromBody] UpdateTemplateDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { error = "User ID not found in token" });

            var template = await _templateService.UpdateAsync(id, dto, userId.Value);
            return Ok(template);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Template not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (ForbiddenAccessException ex)
        {
            _logger.LogWarning("Forbidden update attempt: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message, errors = ex.Errors });
        }
    }

    /// <summary>
    /// Delete a template.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { error = "User ID not found in token" });

            await _templateService.DeleteAsync(id, userId.Value);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Template not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (ForbiddenAccessException ex)
        {
            _logger.LogWarning("Forbidden delete attempt: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Clone a template.
    /// </summary>
    [HttpPost("{id}/clone")]
    [Authorize]
    [ProducesResponseType(typeof(TemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TemplateDto>> Clone(Guid id, [FromBody] CloneTemplateDto? dto = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { error = "User ID not found in token" });

            var template = await _templateService.CloneAsync(id, userId.Value, dto?.NewName);
            return CreatedAtAction(nameof(GetById), new { id = template.Id }, template);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Template not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (ForbiddenAccessException ex)
        {
            _logger.LogWarning("Forbidden clone attempt: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return null;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

/// <summary>
/// DTO for cloning a template.
/// </summary>
public class CloneTemplateDto
{
    public string? NewName { get; set; }
}

