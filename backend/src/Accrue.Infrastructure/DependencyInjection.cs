using Anthropic;
using Accrue.Application.Abstractions;
using Accrue.Application.Extraction;
using Accrue.Application.Reporting;
using Accrue.Infrastructure.Extraction;
using Accrue.Infrastructure.Identity;
using Accrue.Infrastructure.Jobs;
using Accrue.Infrastructure.Persistence;
using Accrue.Infrastructure.Persistence.Interceptors;
using Accrue.Infrastructure.Reporting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Accrue.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Accrue") is { Length: > 0 } configured
            ? configured
            : throw new InvalidOperationException("Connection string 'Accrue' is not configured.");

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<AuditInterceptor>();
        services.AddDbContext<AccrueDbContext>((sp, options) => options
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(5))
            .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));
        services.AddScoped<IAccrueDb>(sp => sp.GetRequiredService<AccrueDbContext>());

        services.AddIdentityApiEndpoints<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 10;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AccrueDbContext>();

        services.AddScoped<IJobScheduler, HangfireJobScheduler>();

        var extraction = configuration.GetSection(ClaudeExtractionOptions.Section).Get<ClaudeExtractionOptions>() ?? new ClaudeExtractionOptions();
        extraction.ApiKey ??= Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrWhiteSpace(extraction.ApiKey))
        {
            services.AddSingleton<IInvoiceDocumentReader, UnavailableInvoiceDocumentReader>();
        }
        else
        {
            services.AddSingleton(extraction);
            services.AddSingleton(new AnthropicClient { ApiKey = extraction.ApiKey, Timeout = TimeSpan.FromSeconds(extraction.TimeoutSeconds) });
            services.AddSingleton<IInvoiceDocumentReader, ClaudeInvoiceDocumentReader>();
        }

        services.AddSingleton(new ReportingOptions());
#if TELERIK
        services.AddScoped<IReportRenderer, TelerikReportRenderer>();
#else
        services.AddScoped<IReportRenderer>(sp => sp.GetRequiredService<CsvReportRenderer>());
#endif
        return services;
    }
}
