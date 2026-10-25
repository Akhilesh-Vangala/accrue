using Accrue.Domain.Common;
using Accrue.Domain.Invoices;

namespace Accrue.Domain.Vendors;

public sealed class Vendor : Entity
{
    private readonly List<Invoice> _invoices = [];

    private Vendor()
    {
        Name = string.Empty;
        TaxId = string.Empty;
        Email = string.Empty;
        Category = string.Empty;
    }

    public Vendor(string name, string taxId, string email, string category, int paymentTermsDays)
    {
        Name = name;
        TaxId = taxId;
        Email = email;
        Category = category;
        PaymentTermsDays = paymentTermsDays;
        IsActive = true;
    }

    public string Name { get; private set; }

    public string TaxId { get; private set; }

    public string Email { get; private set; }

    public string Category { get; private set; }

    /// <summary>Net terms used to derive invoice due dates (Net 30, Net 45, ...).</summary>
    public int PaymentTermsDays { get; private set; }

    public bool IsActive { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<Invoice> Invoices => _invoices.AsReadOnly();

    public void Update(string name, string taxId, string email, string category, int paymentTermsDays)
    {
        Name = name;
        TaxId = taxId;
        Email = email;
        Category = category;
        PaymentTermsDays = paymentTermsDays;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
