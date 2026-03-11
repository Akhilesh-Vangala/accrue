using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LedgerFlow.Application.Common;
using LedgerFlow.Application.Invoices;
using LedgerFlow.Application.Vendors;
using LedgerFlow.Domain.Invoices;

namespace LedgerFlow.IntegrationTests;

internal static class Http
{
    private static int _sequence;

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        await response.ShouldBeAsync(expected);
        return (await response.Content.ReadFromJsonAsync<T>(ApiFactory.Json))!;
    }

    public static async Task ShouldBeAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected {(int)expected} but got {(int)response.StatusCode}: {body}");
        }
    }

    /// <summary>Asserts a problem-details response and returns its machine-readable <c>code</c>.</summary>
    public static async Task<string> ProblemCodeAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        await response.ShouldBeAsync(expected);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("code").GetString()!;
    }

    public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string url, object? body = null) =>
        client.PostAsJsonAsync(url, body ?? new { }, ApiFactory.Json);

    public static async Task<VendorDto> AnyActiveVendorAsync(this HttpClient client)
    {
        var page = await (await client.GetAsync("/api/vendors?pageSize=100")).ReadAsync<PagedResult<VendorDto>>();
        return page.Items.First(v => v.IsActive);
    }

    public static string UniqueNumber(string prefix = "IT") => $"{prefix}-{Environment.TickCount64 % 100_000}-{Interlocked.Increment(ref _sequence)}";

    public static SaveInvoiceRequest NewInvoice(int vendorId, decimal amount, string? number = null) => new(
        vendorId,
        number ?? UniqueNumber(),
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3),
        0m,
        "Integration test",
        [new InvoiceLineInput("Consulting", "6200", 1, amount)]);

    public static async Task<InvoiceDetailDto> CreateSubmittedInvoiceAsync(this HttpClient client, decimal amount)
    {
        var vendor = await client.AnyActiveVendorAsync();
        var created = await (await client.PostJsonAsync("/api/invoices", NewInvoice(vendor.Id, amount)))
            .ReadAsync<InvoiceDetailDto>(HttpStatusCode.Created);
        var submitted = await (await client.PostJsonAsync($"/api/invoices/{created.Summary.Id}/submit"))
            .ReadAsync<InvoiceDetailDto>();
        Assert.Equal(InvoiceStatus.Submitted, submitted.Summary.Status);
        return submitted;
    }
}
