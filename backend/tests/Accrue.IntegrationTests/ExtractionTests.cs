using System.Net;
using System.Net.Http.Headers;
using Accrue.Application.Common;
using Accrue.Application.Extraction;
using Accrue.Application.Invoices;

namespace Accrue.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ExtractionTests(ApiFactory api)
{
    [Fact]
    public async Task Capabilities_report_the_reader_to_invoice_entry_roles_only()
    {
        var caps = await (await api.Clerk.GetAsync("/api/invoices/extraction")).ReadAsync<ExtractionCapabilitiesDto>();
        Assert.True(caps.Enabled);
        Assert.Equal("fake-reader", caps.Model);

        await (await api.Approver.GetAsync("/api/invoices/extraction")).ShouldBeAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Upload_returns_a_matched_draft_without_saving_anything()
    {
        var vendor = await api.Clerk.AnyActiveVendorAsync();
        api.Reader.Next = new ExtractedInvoice(
            vendor.Name.ToUpperInvariant() + ", INC.", vendor.TaxId, "AI-" + Http.UniqueNumber(), new DateOnly(2026, 9, 1), null, "USD",
            250m, 20m, 270m, [new ExtractedLine("Support renewal", 2, 125m, 250m, null)], ["tax_amount"]);
        var before = await CountInvoicesAsync();

        var result = await (await UploadAsync(api.Clerk, "invoice.pdf", "application/pdf")).ReadAsync<InvoiceExtractionDto>();

        Assert.Equal(vendor.Id, result.Draft.VendorId);
        Assert.Equal("tax_id", result.VendorMatchedBy);
        Assert.Equal(20m, result.Draft.TaxAmount);
        Assert.False(string.IsNullOrEmpty(Assert.Single(result.Draft.Lines).GlAccount));
        Assert.Equal(["tax_amount"], result.UncertainFields);
        Assert.Equal("gl_defaulted", Assert.Single(result.Findings).Code);
        Assert.Equal("fake-reader", result.Usage.Model);
        Assert.Equal("application/pdf", api.Reader.LastDocument!.MediaType);
        Assert.Equal(before, await CountInvoicesAsync());
    }

    [Fact]
    public async Task Duplicate_unknown_vendor_and_bad_totals_are_flagged()
    {
        var vendor = await api.Clerk.AnyActiveVendorAsync();
        var existing = await api.Clerk.CreateSubmittedInvoiceAsync(400m);
        api.Reader.Next = new ExtractedInvoice(
            vendor.Name, null, existing.Summary.InvoiceNumber, new DateOnly(2026, 9, 1), null, "USD",
            400m, 0m, 450m, [new ExtractedLine("Consulting", 1, 400m, 400m, "6300")], []);

        var duplicate = await (await UploadAsync(api.Clerk, "dup.pdf", "application/pdf")).ReadAsync<InvoiceExtractionDto>();
        Assert.Contains(duplicate.Findings, f => f.Code == "total_mismatch");
        Assert.Equal(vendor.Id, existing.Summary.VendorId);
        Assert.Contains(duplicate.Findings, f => f.Code == "duplicate_invoice");

        api.Reader.Next = api.Reader.Next with { VendorName = "Unheard Of Supplies Ltd", Total = 400m };
        var unknown = await (await UploadAsync(api.Clerk, "x.pdf", "application/pdf")).ReadAsync<InvoiceExtractionDto>();
        Assert.Null(unknown.Draft.VendorId);
        Assert.Equal("vendor_not_found", unknown.Findings[0].Code);
    }

    [Fact]
    public async Task Unsupported_files_are_rejected_before_the_model_is_called()
    {
        var response = await UploadAsync(api.Clerk, "invoice.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        Assert.Equal("unsupported_file_type", await response.ProblemCodeAsync(HttpStatusCode.Conflict));
    }

    private static Task<HttpResponseMessage> UploadAsync(HttpClient client, string fileName, string mediaType)
    {
        var file = new ByteArrayContent([37, 80, 68, 70, 45]);
        file.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        var form = new MultipartFormDataContent { { file, "file", fileName } };
        return client.PostAsync("/api/invoices/extraction", form);
    }

    private async Task<int> CountInvoicesAsync() =>
        (await (await api.Clerk.GetAsync("/api/invoices?pageSize=1")).ReadAsync<PagedResult<InvoiceSummaryDto>>()).TotalCount;
}
