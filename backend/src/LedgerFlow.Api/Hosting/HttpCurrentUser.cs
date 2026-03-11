using System.Security.Claims;
using LedgerFlow.Application.Abstractions;

namespace LedgerFlow.Api.Hosting;

/// <summary>Resolves the caller from the HTTP context; background jobs (no HTTP context) run as "system".</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string Email =>
        IsAuthenticated
            ? Principal!.FindFirstValue(ClaimTypes.Email) ?? Principal!.FindFirstValue(ClaimTypes.Name) ?? "unknown"
            : "system";

    public IReadOnlyCollection<string> Roles =>
        IsAuthenticated ? Principal!.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() : [];
}
