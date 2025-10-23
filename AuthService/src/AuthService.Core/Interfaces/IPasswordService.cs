namespace AuthService.Core.Interfaces;

/// <summary>
/// Service interface for password operations.
/// </summary>
public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    (bool IsValid, string? Error) ValidatePassword(string password);
}
