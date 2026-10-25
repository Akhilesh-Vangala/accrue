using System.Net;
using System.Net.Http.Json;
using LedgerFlow.Application.Invoices;
using LedgerFlow.Application.Jobs;
using LedgerFlow.Application.Notifications;
using LedgerFlow.Application.Payments;
using LedgerFlow.Domain.Invoices;
using LedgerFlow.Domain.Payments;
using Microsoft.Extensions.DependencyInjection;

namespace LedgerFlow.IntegrationTests;

[Collection(ApiCollection.Name)]
public class WorkflowTests(ApiFactory api)
{
    [Fact]
    public async Task Invoice_moves_from_entry_through_approval_to_payment()
    {
        var invoice = await api.Clerk.CreateSubmittedInvoiceAsync(12_500m);
        var id = invoice.Summary.Id;

        var limit = await api.Approver.PostJsonAsync($"/api/invoices/{id}/approve", new DecisionRequest(null));
        Assert.Equal("approval_limit_exceeded", await limit.ProblemCodeAsync(HttpStatusCode.Conflict));

        var approved = await (await api.Controller.PostJsonAsync($"/api/invoices/{id}/approve", new DecisionRequest("Budgeted")))
            .ReadAsync<InvoiceDetailDto>();
        Assert.Equal(InvoiceStatus.Approved, approved.Summary.Status);

        var paymentDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var run = await (await api.Clerk.PostJsonAsync("/api/payment-runs", new CreatePaymentRunRequest("IT run", paymentDate, [id])))
            .ReadAsync<PaymentRunDetailDto>(HttpStatusCode.Created);
        Assert.Equal(12_500m, run.Summary.Total);

        var clerkRelease = await api.Clerk.PostJsonAsync($"/api/payment-runs/{run.Summary.Id}/release");
        await clerkRelease.ShouldBeAsync(HttpStatusCode.Forbidden);

        var released = await (await api.Controller.PostJsonAsync($"/api/payment-runs/{run.Summary.Id}/release"))
            .ReadAsync<PaymentRunDetailDto>();
        Assert.Equal(PaymentRunStatus.Released, released.Summary.Status);
        Assert.Equal($"ACH-{paymentDate:yyyyMMdd}-{run.Summary.Id:D4}-001", Assert.Single(released.Payments).Reference);

        var paid = await (await api.Clerk.GetAsync($"/api/invoices/{id}")).ReadAsync<InvoiceDetailDto>();
        Assert.Equal(InvoiceStatus.Paid, paid.Summary.Status);
        Assert.Equal(0m, paid.Summary.Outstanding);
        Assert.Equal(
            [InvoiceEventType.Created, InvoiceEventType.Submitted, InvoiceEventType.Approved, InvoiceEventType.Scheduled, InvoiceEventType.Paid],
            paid.Events.OrderBy(e => e.OccurredAt).Select(e => e.Type));
    }

    [Fact]
    public async Task Decision_notification_job_tells_the_clerk()
    {
        var invoice = await api.Clerk.CreateSubmittedInvoiceAsync(800m);
        var id = invoice.Summary.Id;
        await (await api.Approver.PostJsonAsync($"/api/invoices/{id}/reject", new DecisionRequest("Missing PO"))).ShouldBeAsync(HttpStatusCode.OK);

        // Hangfire's server is off in tests, so run the enqueued job body directly.
        await api.RunInScopeAsync(async sp =>
        {
            await sp.GetRequiredService<InvoiceDecisionNotificationJob>().RunAsync(id, CancellationToken.None);
            return true;
        });

        var feed = await (await api.Clerk.GetAsync("/api/notifications")).ReadAsync<NotificationFeedDto>();
        Assert.Contains(feed.Items, n => n.Body.Contains(invoice.Summary.InvoiceNumber, StringComparison.Ordinal) && n.Body.Contains("Missing PO", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Person_who_entered_an_invoice_cannot_approve_it()
    {
        var invoice = await api.Controller.CreateSubmittedInvoiceAsync(500m);

        var response = await api.Controller.PostJsonAsync($"/api/invoices/{invoice.Summary.Id}/approve", new DecisionRequest(null));

        Assert.Equal("segregation_of_duties", await response.ProblemCodeAsync(HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Controller_who_edited_a_clerks_invoice_cannot_approve_it()
    {
        var vendor = await api.Clerk.AnyActiveVendorAsync();
        var draft = await (await api.Clerk.PostJsonAsync("/api/invoices", Http.NewInvoice(vendor.Id, 700m)))
            .ReadAsync<InvoiceDetailDto>(HttpStatusCode.Created);
        var id = draft.Summary.Id;

        var edit = Http.NewInvoice(vendor.Id, 9_500m, draft.Summary.InvoiceNumber) with { RowVersion = draft.RowVersion };
        await (await api.Controller.PutAsJsonAsync($"/api/invoices/{id}", edit, ApiFactory.Json)).ShouldBeAsync(HttpStatusCode.OK);
        await (await api.Clerk.PostJsonAsync($"/api/invoices/{id}/submit")).ShouldBeAsync(HttpStatusCode.OK);

        var queue = await (await api.Controller.GetAsync("/api/invoices/approval-queue")).ReadAsync<List<InvoiceSummaryDto>>();
        Assert.DoesNotContain(queue, i => i.Id == id);

        var response = await api.Controller.PostJsonAsync($"/api/invoices/{id}/approve", new DecisionRequest(null));
        Assert.Equal("segregation_of_duties", await response.ProblemCodeAsync(HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Rejection_without_reason_is_refused()
    {
        var invoice = await api.Clerk.CreateSubmittedInvoiceAsync(500m);

        var response = await api.Approver.PostJsonAsync($"/api/invoices/{invoice.Summary.Id}/reject", new DecisionRequest(" "));

        Assert.Equal("rejection_reason_required", await response.ProblemCodeAsync(HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Duplicate_vendor_invoice_number_is_rejected()
    {
        var vendor = await api.Clerk.AnyActiveVendorAsync();
        var request = Http.NewInvoice(vendor.Id, 100m, Http.UniqueNumber("DUP"));
        await (await api.Clerk.PostJsonAsync("/api/invoices", request)).ShouldBeAsync(HttpStatusCode.Created);

        var duplicate = await api.Clerk.PostJsonAsync("/api/invoices", request);

        await duplicate.ShouldBeAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Invalid_invoice_returns_field_errors()
    {
        var vendor = await api.Clerk.AnyActiveVendorAsync();
        var request = Http.NewInvoice(vendor.Id, 100m) with { Lines = [new InvoiceLineInput("", "12", 0, 5m)] };

        var response = await api.Clerk.PostJsonAsync("/api/invoices", request);

        Assert.Equal("validation_failed", await response.ProblemCodeAsync(HttpStatusCode.BadRequest));
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("lines[0].Description", body, StringComparison.Ordinal);
        Assert.Contains("lines[0].GlAccount", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Approver_cannot_enter_invoices()
    {
        var vendor = await api.Approver.AnyActiveVendorAsync();

        var response = await api.Approver.PostJsonAsync("/api/invoices", Http.NewInvoice(vendor.Id, 100m));

        await response.ShouldBeAsync(HttpStatusCode.Forbidden);
    }
}
