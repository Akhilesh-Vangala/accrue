using LedgerFlow.Domain.Common;
using LedgerFlow.Domain.Invoices;
using static LedgerFlow.UnitTests.TestData;

namespace LedgerFlow.UnitTests;

public class InvoiceTests
{
    [Fact]
    public void New_invoice_is_a_draft_with_totals_and_due_date_from_vendor_terms()
    {
        var invoice = new Invoice(
            Vendor(termsDays: 45),
            " INV-1 ",
            new DateOnly(2026, 9, 1),
            8.25m,
            [new InvoiceLineInput("Toner", "6100", 3, 33.335m), new InvoiceLineInput("Paper", "6100", 2, 10m)],
            Clerk,
            Now);

        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        Assert.Equal("INV-1", invoice.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 10, 16), invoice.DueDate);
        Assert.Equal(120.01m, invoice.Subtotal);
        Assert.Equal(128.26m, invoice.Total);
        Assert.Equal(invoice.Total, invoice.Outstanding);
        Assert.Equal(InvoiceEventType.Created, Assert.Single(invoice.Events).Type);
    }

    [Fact]
    public void Inactive_vendor_cannot_receive_invoices()
    {
        var vendor = Vendor();
        vendor.Deactivate();

        var error = Assert.Throws<DomainException>(() => Invoice(vendor: vendor));
        Assert.Equal("vendor_inactive", error.Code);
    }

    [Fact]
    public void Submitting_requires_a_positive_total()
    {
        var invoice = Invoice(unitPrice: 0m);

        var error = Assert.Throws<DomainException>(() => invoice.Submit(Clerk, Now));
        Assert.Equal("invoice_empty", error.Code);
    }

    [Fact]
    public void Approver_can_approve_up_to_the_limit()
    {
        var invoice = Submitted(unitPrice: ApprovalPolicy.ApproverLimit);

        invoice.Approve(Approver, ApproverRoles, "ok", Now);

        Assert.Equal(InvoiceStatus.Approved, invoice.Status);
        Assert.Equal("ok", invoice.Events.Last().Comment);
    }

    [Fact]
    public void Approver_cannot_approve_above_the_limit_but_controller_can()
    {
        var invoice = Submitted(unitPrice: ApprovalPolicy.ApproverLimit + 0.01m);

        var error = Assert.Throws<DomainException>(() => invoice.Approve(Approver, ApproverRoles, null, Now));
        Assert.Equal("approval_limit_exceeded", error.Code);
        Assert.Equal(InvoiceStatus.Submitted, invoice.Status);

        invoice.Approve(Controller, ControllerRoles, null, Now);
        Assert.Equal(InvoiceStatus.Approved, invoice.Status);
    }

    [Fact]
    public void Clerk_role_cannot_approve()
    {
        var invoice = Submitted();

        var error = Assert.Throws<DomainException>(() => invoice.Approve("other.clerk@ledgerflow.dev", ClerkRoles, null, Now));
        Assert.Equal("approval_forbidden", error.Code);
    }

    [Theory]
    [InlineData(Clerk)]
    [InlineData("CLERK@LEDGERFLOW.DEV")]
    public void Person_who_entered_the_invoice_cannot_decide_on_it(string actor)
    {
        var invoice = Submitted();

        Assert.Equal("segregation_of_duties", Assert.Throws<DomainException>(() => invoice.Approve(actor, ControllerRoles, null, Now)).Code);
        Assert.Equal("segregation_of_duties", Assert.Throws<DomainException>(() => invoice.Reject(actor, "no", Now)).Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejection_needs_a_reason(string reason)
    {
        var invoice = Submitted();

        var error = Assert.Throws<DomainException>(() => invoice.Reject(Approver, reason, Now));
        Assert.Equal("rejection_reason_required", error.Code);
    }

    [Fact]
    public void Rejected_invoice_can_be_corrected_and_resubmitted()
    {
        var invoice = Submitted();
        invoice.Reject(Approver, "Wrong PO number", Now);

        invoice.UpdateDraft(invoice.InvoiceDate, 0m, [new InvoiceLineInput("Paper", "6100", 1, 900m)], "Fixed PO", Clerk, Now);
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);

        invoice.Submit(Clerk, Now);
        Assert.Equal(InvoiceStatus.Submitted, invoice.Status);
        Assert.Equal(900m, invoice.Total);
    }

    [Fact]
    public void Approved_invoice_is_no_longer_editable()
    {
        var invoice = Approved();

        var error = Assert.Throws<DomainException>(() =>
            invoice.UpdateDraft(invoice.InvoiceDate, 0m, [new InvoiceLineInput("x", "6100", 1, 1m)], null, Clerk, Now));
        Assert.Equal("invoice_not_editable", error.Code);
    }

    [Fact]
    public void Overdue_flag_is_raised_once_and_ignores_drafts()
    {
        var draft = Invoice();
        var submitted = Submitted();
        var afterDue = submitted.DueDate.AddDays(1);

        Assert.False(draft.RefreshOverdue(afterDue, Now));
        Assert.True(submitted.RefreshOverdue(afterDue, Now));
        Assert.True(submitted.IsOverdue);
        Assert.False(submitted.RefreshOverdue(afterDue, Now));
        Assert.Single(submitted.Events, e => e.Type == InvoiceEventType.FlaggedOverdue);
    }

    [Fact]
    public void Invoice_is_not_overdue_on_its_due_date()
    {
        var invoice = Submitted();

        Assert.False(invoice.RefreshOverdue(invoice.DueDate, Now));
        Assert.False(invoice.IsOverdue);
    }
}
