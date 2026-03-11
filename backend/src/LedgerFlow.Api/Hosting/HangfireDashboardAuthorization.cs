using Hangfire.Dashboard;
using LedgerFlow.Domain.Common;

namespace LedgerFlow.Api.Hosting;

/// <summary>Only signed-in controllers can open /hangfire. The SPA's auth cookie is same-origin, so this works from the app.</summary>
public sealed class HangfireDashboardAuthorization : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var user = context.GetHttpContext().User;
        return user.Identity?.IsAuthenticated == true && user.IsInRole(Roles.Controller);
    }
}
