namespace AuthService.Core.DTOs;

/// <summary>
/// Data transfer object containing user information.
/// </summary>
public class UserInfoDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool MfaEnabled { get; set; }

    public UserInfoDto()
    {
    }

    public UserInfoDto(Guid id, string username, string email, bool emailVerified, 
        string? firstName, string? lastName, string? avatarUrl, List<string> roles, bool mfaEnabled)
    {
        Id = id;
        Username = username;
        Email = email;
        EmailVerified = emailVerified;
        FirstName = firstName;
        LastName = lastName;
        AvatarUrl = avatarUrl;
        Roles = roles;
        MfaEnabled = mfaEnabled;
    }
}

