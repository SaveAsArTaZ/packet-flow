namespace AuthService.Core.Exceptions;

/// <summary>
/// Exception thrown when authentication fails.
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException() : base("Unauthorized access")
    {
    }

    public UnauthorizedException(string message) : base(message)
    {
    }

    public UnauthorizedException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}


