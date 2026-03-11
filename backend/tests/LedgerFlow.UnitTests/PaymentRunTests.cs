using LedgerFlow.Domain.Common;
using LedgerFlow.Domain.Invoices;
using LedgerFlow.Domain.Payments;
using static LedgerFlow.UnitTests.TestData;

namespace LedgerFlow.UnitTests;

public class PaymentRunTests
{
    private static PaymentRun Run(int id = 7) => WithId(new PaymentRun("Weekly run", new DateOnly(2026, 10, 5)), id);

    [Fact]
    public void Run_must_be_persisted_before_invoices_are_added()
    {
        var run = new PaymentRun("Unsaved", new DateOnly(2026, 10, 5));

        Assert.Throws<InvalidOperationException>(() => run.AddInvoice(Approved(), Clerk, Now));
    }

    [Fact]
    public void Only_approved_invoices_can_be_scheduled()
    {
        var error = Assert.Throws<DomainException>(() => Run().AddInvoice(Submitted(), Clerk, Now));
        Assert.Equal("invoice_not_payable", error.Code);
    }

    [Fact]
    public void Adding_invoices_schedules_them_and_totals_the_run()
    {
        var run = Run();
        var first = Approved(1_000m);
        var second = Approved(250.50m);

        run.AddInvoice(first, Clerk, Now);
        run.AddInvoice(second, Clerk, Now);
        run.AddInvoice(first, Clerk, Now);

        Assert.Equal(2, run.Payments.Count);
        Assert.Equal(1_250.50m, run.Total);
        Assert.All([first, second], i =>
        {
            Assert.Equal(InvoiceStatus.Scheduled, i.Status);
            Assert.Equal(run.Id, i.PaymentRunId);
        });
    }

    [Fact]
    public void Only_controllers_can_release()
    {
        var run = Run();
        run.AddInvoice(Approved(), Clerk, Now);

        var error = Assert.Throws<DomainException>(() => run.Release(Approver, ApproverRoles, Now));
        Assert.Equal("release_forbidden", error.Code);
        Assert.Equal(PaymentRunStatus.Draft, run.Status);
    }

    [Fact]
    public void Empty_run_cannot_be_released()
    {
        var error = Assert.Throws<DomainException>(() => Run().Release(Controller, ControllerRoles, Now));
        Assert.Equal("payment_run_empty", error.Code);
    }

    [Fact]
    public void Release_pays_every_invoice_with_a_sequential_ach_reference()
    {
        var run = Run(id: 42);
        var first = Approved(1_000m);
        var second = Approved(500m);
        first.RefreshOverdue(first.DueDate.AddDays(3), Now);
        run.AddInvoice(first, Clerk, Now);
        run.AddInvoice(second, Clerk, Now);

        run.Release(Controller, ControllerRoles, Now);

        Assert.Equal(PaymentRunStatus.Released, run.Status);
        Assert.Equal(Controller, run.ReleasedBy);
        Assert.Equal(
            ["ACH-20261005-0042-001", "ACH-20261005-0042-002"],
            run.Payments.Select(p => p.Reference));
        Assert.All([first, second], i =>
        {
            Assert.Equal(InvoiceStatus.Paid, i.Status);
            Assert.Equal(0m, i.Outstanding);
            Assert.False(i.IsOverdue);
        });
    }

    [Fact]
    public void Released_run_is_locked()
    {
        var run = Run();
        run.AddInvoice(Approved(), Clerk, Now);
        run.Release(Controller, ControllerRoles, Now);

        Assert.Equal("payment_run_locked", Assert.Throws<DomainException>(() => run.Cancel(Controller, Now)).Code);
        Assert.Equal("payment_run_locked", Assert.Throws<DomainException>(() => run.AddInvoice(Approved(), Clerk, Now)).Code);
    }

    [Fact]
    public void Cancel_returns_invoices_to_approved()
    {
        var run = Run();
        var invoice = Approved();
        run.AddInvoice(invoice, Clerk, Now);

        run.Cancel(Controller, Now);

        Assert.Equal(PaymentRunStatus.Cancelled, run.Status);
        Assert.Equal(InvoiceStatus.Approved, invoice.Status);
        Assert.Null(invoice.PaymentRunId);
    }
}
