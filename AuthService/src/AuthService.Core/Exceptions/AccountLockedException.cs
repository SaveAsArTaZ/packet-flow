namespace AuthService.Core.Exceptions;

/// <summary>
/// Exception thrown when an account is locked.
/// </summary>
public class AccountLockedException : Exception
{
    public DateTime? LockoutEnd { get; }

    public AccountLockedException(DateTime? lockoutEnd) 
        : base($"Account is locked until {lockoutEnd}")
    {
        LockoutEnd = lockoutEnd;
    }

    public AccountLockedException(string message) : base(message)
    {
    }
}


