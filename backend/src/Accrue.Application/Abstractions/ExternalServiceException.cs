namespace Accrue.Application.Abstractions;

/// <summary>A dependency outside Accrue (the model API) failed. Surfaced as 502, or 503 when retrying later may help.</summary>
public sealed class ExternalServiceException : Exception
{
    public ExternalServiceException()
    {
        Code = "external_service_error";
    }

    public ExternalServiceException(string message)
        : base(message)
    {
        Code = "external_service_error";
    }

    public ExternalServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
        Code = "external_service_error";
    }

    public ExternalServiceException(string code, string message, bool retryable, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        Retryable = retryable;
    }

    public string Code { get; }

    public bool Retryable { get; }
}
