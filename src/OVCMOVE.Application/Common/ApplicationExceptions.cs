namespace OVCMOVE.Application.Common;

/// <summary>Represents invalid input or an invalid requested state transition.</summary>
public class ApplicationValidationException(string message) : Exception(message);

/// <summary>Represents an application resource that could not be found.</summary>
public class ApplicationNotFoundException(string message) : Exception(message);

/// <summary>Represents an application operation that conflicts with current state.</summary>
public class ApplicationConflictException : Exception
{
    public ApplicationConflictException(string message) : base(message)
    {
    }

    public ApplicationConflictException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Represents an optimistic-concurrency conflict.</summary>
public sealed class ConcurrencyConflictException : ApplicationConflictException
{
    public ConcurrencyConflictException(string message) : base(message)
    {
    }
}

public class ApplicationRateLimitException : Exception
{
    public int RetryAfterSeconds { get; }

    public ApplicationRateLimitException(int retryAfterSeconds, string message) : base(message) 
    { 
        RetryAfterSeconds = retryAfterSeconds;
    }
}

public class ApplicationForbiddenException : Exception
{
    public ApplicationForbiddenException(string message) : base(message) { }
}

public sealed class ApplicationServiceUnavailableException : Exception
{
    public ApplicationServiceUnavailableException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
