using AuthService.Core.DTOs;
using AuthService.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserRepository userRepository, ILogger<UserController> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's profile.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserInfoDto>> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null)
            return NotFound();

        return Ok(new UserInfoDto(
            user.Id,
            user.Username,
            user.Email,
            user.EmailVerified,
            user.FirstName,
            user.LastName,
            user.AvatarUrl,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            user.MfaEnabled
        ));
    }

    /// <summary>
    /// Update user profile.
    /// </summary>
    [HttpPut("profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null)
            return NotFound();

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.AvatarUrl = dto.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        _logger.LogInformation("User profile updated: {UserId}", userId);

        return Ok(new { message = "Profile updated successfully" });
    }

    /// <summary>
    /// Get user by ID (Admin only).
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserInfoDto>> GetUser(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound();

        return Ok(new UserInfoDto(
            user.Id,
            user.Username,
            user.Email,
            user.EmailVerified,
            user.FirstName,
            user.LastName,
            user.AvatarUrl,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            user.MfaEnabled
        ));
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
            return null;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

/// <summary>
/// DTO for updating user profile.
/// </summary>
public record UpdateProfileDto(string? FirstName, string? LastName, string? AvatarUrl);


