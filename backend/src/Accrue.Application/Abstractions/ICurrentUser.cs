namespace Accrue.Application.Abstractions;

/// <summary>The authenticated caller. Jobs run as a named system actor.</summary>
public interface ICurrentUser
{
    string Email { get; }

    IReadOnlyCollection<string> Roles { get; }

    bool IsAuthenticated { get; }
}
