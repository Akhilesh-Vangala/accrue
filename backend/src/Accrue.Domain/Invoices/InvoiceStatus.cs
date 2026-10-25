namespace Accrue.Domain.Invoices;

public enum InvoiceStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Scheduled = 4,
    Paid = 5,
}
