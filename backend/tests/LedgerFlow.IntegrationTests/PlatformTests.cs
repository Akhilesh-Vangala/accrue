using System.Net;
using System.Net.Http.Json;
using LedgerFlow.Application.Abstractions;
using LedgerFlow.Application.Dashboard;
using LedgerFlow.Application.Jobs;
using LedgerFlow.Application.Reporting;
using LedgerFlow.Application.Vendors;
using LedgerFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LedgerFlow.IntegrationTests;

[Collection(ApiCollection.Name)]
public class PlatformTests(ApiFactory api)
{
    [Theory]
    [InlineData("/api/invoices")]
    [InlineData("/api/dashboard")]
    [InlineData("/hangfire")]
    public async Task Anonymous_requests_are_rejected(string url)
    {
        using var anonymous = api.CreateClient();

        var response = await anonymous.GetAsync(url);

        await response.ShouldBeAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Self_registration_is_disabled()
    {
        using var anonymous = api.CreateClient();

        var response = await anonymous.PostJsonAsync("/api/auth/register", new { email = "x@example.com", password = "Whatever!2026" });

        await response.ShouldBeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Health_check_reaches_sql_server()
    {
        using var anonymous = api.CreateClient();

        var response = await anonymous.GetAsync("/health");

        await response.ShouldBeAsync(HttpStatusCode.OK);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Responses_carry_security_headers()
    {
        var response = await api.Clerk.GetAsync("/api/auth/me");

        await response.ShouldBeAsync(HttpStatusCode.OK);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }

    [Fact]
    public async Task Stale_vendor_edit_is_rejected()
    {
        var taxId = $"{Random.Shared.Next(10, 99)}-{Random.Shared.Next(1_000_000, 9_999_999)}";
        var created = await (await api.Clerk.PostJsonAsync("/api/vendors", new SaveVendorRequest("Concurrency Co", taxId, "ap@concurrency.test", "Software", 30)))
            .ReadAsync<VendorDto>(HttpStatusCode.Created);

        var first = new SaveVendorRequest("Concurrency Co (A)", taxId, "ap@concurrency.test", "Software", 45, created.RowVersion);
        await (await api.Clerk.PutAsJsonAsync($"/api/vendors/{created.Id}", first, ApiFactory.Json)).ShouldBeAsync(HttpStatusCode.OK);

        var stale = first with { Name = "Concurrency Co (B)" };
        var response = await api.Controller.PutAsJsonAsync($"/api/vendors/{created.Id}", stale, ApiFactory.Json);

        Assert.Equal("concurrency_conflict", await response.ProblemCodeAsync(HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Dashboard_returns_all_aging_buckets()
    {
        var dashboard = await (await api.Controller.GetAsync("/api/dashboard")).ReadAsync<DashboardDto>();

        Assert.Equal(DashboardService.Buckets.Select(b => b.Bucket), dashboard.Aging.Select(a => a.Bucket));
        Assert.True(dashboard.Kpis.OpenPayables > 0);
    }

    [Fact]
    public async Task Jobs_are_controller_only()
    {
        await (await api.Clerk.GetAsync("/api/jobs")).ShouldBeAsync(HttpStatusCode.Forbidden);

        var jobs = await (await api.Controller.GetAsync("/api/jobs")).ReadAsync<List<RecurringJobInfo>>();

        Assert.Equal(JobCatalog.Recurring.Keys.Order(), jobs.Select(j => j.Id).Order());
    }

    [Fact]
    public async Task Aging_snapshot_job_is_idempotent()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var counts = await api.RunInScopeAsync(async sp =>
        {
            var job = sp.GetRequiredService<AgingSnapshotJob>();
            var db = sp.GetRequiredService<LedgerFlowDbContext>();
            await job.RunAsync(CancellationToken.None);
            var once = await db.AgingSnapshots.CountAsync(s => s.SnapshotDate == today);
            await job.RunAsync(CancellationToken.None);
            var twice = await db.AgingSnapshots.CountAsync(s => s.SnapshotDate == today);
            return (once, twice);
        });

        Assert.Equal(DashboardService.Buckets.Count, counts.once);
        Assert.Equal(counts.once, counts.twice);
    }

    [Fact]
    public async Task Aging_report_downloads_as_csv()
    {
        var response = await api.Controller.GetAsync($"/api/reports/{ReportNames.ApAging}.csv");

        await response.ShouldBeAsync(HttpStatusCode.OK);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var csv = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("Vendor", csv.TrimStart('\uFEFF'), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Pdf_without_telerik_explains_itself()
    {
        var capabilities = await (await api.Controller.GetAsync("/api/reports/capabilities")).ReadAsync<ReportCapabilitiesDto>();
        if (capabilities.SupportsPdf)
        {
            return;
        }

        var response = await api.Controller.GetAsync($"/api/reports/{ReportNames.ApAging}.pdf");

        Assert.Equal("pdf_unavailable", await response.ProblemCodeAsync(HttpStatusCode.Conflict));
    }
}
