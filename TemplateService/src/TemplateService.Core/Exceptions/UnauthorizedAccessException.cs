namespace TemplateService.Core.Exceptions;

/// <summary>
/// Exception thrown when a user attempts an unauthorized action.
/// </summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message) : base(message)
    {
    }

    public ForbiddenAccessException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}

