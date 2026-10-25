namespace Accrue.Domain.Common;

/// <summary>A business rule was violated. Surfaced to clients as 409 Conflict with <see cref="Code"/>.</summary>
public sealed class DomainException : Exception
{
    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public DomainException()
    {
        Code = "domain_error";
    }

    public DomainException(string message)
        : base(message)
    {
        Code = "domain_error";
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
        Code = "domain_error";
    }

    public string Code { get; }
}
