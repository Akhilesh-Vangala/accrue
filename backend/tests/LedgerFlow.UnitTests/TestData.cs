using LedgerFlow.Domain.Common;
using LedgerFlow.Domain.Invoices;
using LedgerFlow.Domain.Vendors;

namespace LedgerFlow.UnitTests;

internal static class TestData
{
    public const string Clerk = "clerk@ledgerflow.dev";
    public const string Approver = "approver@ledgerflow.dev";
    public const string Controller = "controller@ledgerflow.dev";

    public static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    public static readonly string[] ApproverRoles = [Roles.Approver];
    public static readonly string[] ControllerRoles = [Roles.Controller];
    public static readonly string[] ClerkRoles = [Roles.Clerk];

    private static int _nextId = 1000;

    public static Vendor Vendor(int termsDays = 30) =>
        WithId(new Vendor("Contoso Supplies", "12-3456789", "ap@contoso.test", "Software", termsDays));

    public static Invoice Invoice(decimal unitPrice = 1_000m, decimal quantity = 1, decimal tax = 0m, Vendor? vendor = null) =>
        WithId(new Invoice(
            vendor ?? Vendor(),
            $"INV-{Interlocked.Increment(ref _nextId)}",
            new DateOnly(2026, 9, 1),
            tax,
            [new InvoiceLineInput("Paper", "6100", quantity, unitPrice)],
            Clerk,
            Now));

    public static Invoice Submitted(decimal unitPrice = 1_000m)
    {
        var invoice = Invoice(unitPrice);
        invoice.Submit(Clerk, Now);
        return invoice;
    }

    public static Invoice Approved(decimal unitPrice = 1_000m)
    {
        var invoice = Submitted(unitPrice);
        invoice.Approve(Controller, ControllerRoles, null, Now);
        return invoice;
    }

    /// <summary>Simulates the identity EF Core assigns on insert.</summary>
    public static T WithId<T>(T entity, int? id = null)
        where T : Entity
    {
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id ?? Interlocked.Increment(ref _nextId));
        return entity;
    }
}
