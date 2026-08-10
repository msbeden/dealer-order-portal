namespace DealerPortal.Domain.Exceptions;

/// <summary>
/// Domain layer exception thrown when business rules or invariants are violated.
/// </summary>
public class DomainException : Exception
{
    public string ErrorCode { get; }

    public DomainException(string message, string errorCode = "DOMAIN_RULE_VIOLATION")
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public DomainException(string message, Exception innerException, string errorCode = "DOMAIN_RULE_VIOLATION")
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
