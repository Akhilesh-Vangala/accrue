using LedgerFlow.Application.Abstractions;
using LedgerFlow.Application.Reporting;
using LedgerFlow.Infrastructure.Identity;
using LedgerFlow.Infrastructure.Jobs;
using LedgerFlow.Infrastructure.Persistence;
using LedgerFlow.Infrastructure.Persistence.Interceptors;
using LedgerFlow.Infrastructure.Reporting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LedgerFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LedgerFlow") is { Length: > 0 } configured
            ? configured
            : throw new InvalidOperationException("Connection string 'LedgerFlow' is not configured.");

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<AuditInterceptor>();
        services.AddDbContext<LedgerFlowDbContext>((sp, options) => options
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(5))
            .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));
        services.AddScoped<ILedgerFlowDb>(sp => sp.GetRequiredService<LedgerFlowDbContext>());

        services.AddIdentityApiEndpoints<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 10;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<LedgerFlowDbContext>();

        services.AddScoped<IJobScheduler, HangfireJobScheduler>();

        services.AddSingleton(new ReportingOptions());
#if TELERIK
        services.AddScoped<IReportRenderer, TelerikReportRenderer>();
#else
        services.AddScoped<IReportRenderer>(sp => sp.GetRequiredService<CsvReportRenderer>());
#endif
        return services;
    }
}
