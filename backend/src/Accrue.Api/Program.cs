using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.SqlServer;
using Accrue.Api.Hosting;
using Accrue.Application;
using Accrue.Application.Abstractions;
using Accrue.Infrastructure;
using Accrue.Infrastructure.Identity;
using Accrue.Infrastructure.Jobs;
using Accrue.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
#if TELERIK
using Accrue.Infrastructure.Reporting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telerik.Reporting.Cache.File;
using Telerik.Reporting.Services;
#endif

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var connectionString = config.GetConnectionString("Accrue") is { Length: > 0 } configured
    ? configured
    : throw new InvalidOperationException("Connection string 'Accrue' is not configured.");

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(config);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "accrue.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();

var mvc = builder.Services.AddControllers();
#if TELERIK
// The Telerik REST service serializes with Json.NET, so the whole MVC pipeline uses it when Telerik is enabled.
mvc.AddNewtonsoftJson(o =>
{
    o.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
    o.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
});
#else
mvc.AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
#endif
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi(o => o.AddDocumentTransformer((doc, _, _) =>
{
    doc.Info.Title = "Accrue API";
    doc.Info.Description = "Accounts payable: vendors, invoice approvals, payment runs, reporting and background jobs.";
    return Task.CompletedTask;
}));
builder.Services.AddHealthChecks().AddDbContextCheck<AccrueDbContext>("sql");

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

    // Each extraction is a paid model call; cap it per signed-in user.
    o.AddPolicy("extraction", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.User.Identity?.Name ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddHangfire(h => h
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        SchemaName = "hangfire",
        PrepareSchemaIfNecessary = true,
        QueuePollInterval = TimeSpan.FromSeconds(5),
    }));
if (config.GetValue("Hangfire:ServerEnabled", true))
{
    builder.Services.AddHangfireServer(o => o.WorkerCount = Math.Min(Environment.ProcessorCount * 2, 8));
}

#if TELERIK
System.Data.Common.DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", Microsoft.Data.SqlClient.SqlClientFactory.Instance);
builder.Services.TryAddSingleton<IReportServiceConfiguration>(sp => new ReportServiceConfiguration
{
    ReportingEngineConfiguration = sp.GetRequiredService<IConfiguration>(),
    HostAppId = "Accrue",
    Storage = new FileStorage(),
    ReportSourceResolver = new UriReportSourceResolver(sp.GetRequiredService<ReportingOptions>().ReportsPath),
});
#endif

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    // Runs before anything resolves Hangfire storage, which needs the database to exist.
    if (config.GetValue("Database:MigrateOnStartup", true))
    {
        await scope.ServiceProvider.GetRequiredService<AccrueDbContext>().Database.MigrateAsync();
    }

    if (config.GetValue("Database:Seed", false))
    {
        await DbSeeder.SeedAsync(scope.ServiceProvider);
    }
}

app.UseExceptionHandler();
app.UseStatusCodePages();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.XContentTypeOptions = "nosniff";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers.XFrameOptions = "DENY";
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/api/auth")
    .MapIdentityApi<AppUser>()
    .AddEndpointFilter(async (ctx, next) =>
        ctx.HttpContext.Request.Path.Value?.EndsWith("/register", StringComparison.OrdinalIgnoreCase) == true
            ? Results.NotFound()
            : await next(ctx))
    .RequireRateLimiting("login")
    .WithTags("Auth");

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorization()],
    DashboardTitle = "Accrue jobs",
    AppPath = "/",
});

if (app.Environment.IsDevelopment() || config.GetValue("OpenApi:Enabled", false))
{
    app.MapOpenApi();
    app.MapScalarApiReference(o => o.WithTitle("Accrue API"));
}

app.MapFallbackToFile("{*path:nonfile:regex(^(?!api/|hangfire|health|openapi|scalar).*$)}", "index.html");

HangfireJobScheduler.RegisterRecurringJobs(app.Services.GetRequiredService<IRecurringJobManager>());

await app.RunAsync();

/// <summary>Entry point; public so integration tests can host it with WebApplicationFactory.</summary>
public partial class Program;
