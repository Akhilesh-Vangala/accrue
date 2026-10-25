using Accrue.Domain.Common;
using Accrue.Domain.Vendors;

namespace Accrue.Domain.Invoices;

/// <summary>
/// Vendor invoice aggregate. Every state change goes through a method here so the approval workflow,
/// approval limits, and segregation of duties are enforced in one place.
/// </summary>
public sealed class Invoice : Entity
{
    /// <summary>Events that make someone a preparer of the invoice; preparers may not decide on it.</summary>
    public static readonly InvoiceEventType[] PreparationEvents = [InvoiceEventType.Created, InvoiceEventType.Updated, InvoiceEventType.Submitted];

    private readonly List<InvoiceLine> _lines = [];
    private readonly List<InvoiceEvent> _events = [];

    private Invoice()
    {
        InvoiceNumber = string.Empty;
        Currency = "USD";
    }

    public Invoice(
        Vendor vendor,
        string invoiceNumber,
        DateOnly invoiceDate,
        decimal taxAmount,
        IEnumerable<InvoiceLineInput> lines,
        string actor,
        DateTimeOffset now,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(vendor);
        if (!vendor.IsActive)
        {
            throw new DomainException("vendor_inactive", $"Vendor '{vendor.Name}' is inactive.");
        }

        Vendor = vendor;
        VendorId = vendor.Id;
        InvoiceNumber = invoiceNumber.Trim();
        InvoiceDate = invoiceDate;
        DueDate = invoiceDate.AddDays(vendor.PaymentTermsDays);
        Currency = "USD";
        Notes = notes;
        Status = InvoiceStatus.Draft;
        CreatedBy = actor;
        CreatedAt = now;
        SetLines(lines, taxAmount);
        Record(InvoiceEventType.Created, actor, null, now);
    }

    public int VendorId { get; private set; }

    public Vendor? Vendor { get; private set; }

    public string InvoiceNumber { get; private set; }

    public DateOnly InvoiceDate { get; private set; }

    public DateOnly DueDate { get; private set; }

    public string Currency { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal TaxAmount { get; private set; }

    public decimal Total { get; private set; }

    public decimal AmountPaid { get; private set; }

    public InvoiceStatus Status { get; private set; }

    /// <summary>Maintained by the nightly overdue job; true when unpaid past <see cref="DueDate"/>.</summary>
    public bool IsOverdue { get; private set; }

    public string? Notes { get; private set; }

    public int? PaymentRunId { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<InvoiceLine> Lines => _lines.AsReadOnly();

    public IReadOnlyCollection<InvoiceEvent> Events => _events.AsReadOnly();

    public decimal Outstanding => Total - AmountPaid;

    public bool IsOpen => Status is not (InvoiceStatus.Paid or InvoiceStatus.Rejected);

    public void UpdateDraft(DateOnly invoiceDate, decimal taxAmount, IEnumerable<InvoiceLineInput> lines, string? notes, string actor, DateTimeOffset now)
    {
        EnsureStatus("invoice_not_editable", "Only draft or rejected invoices can be edited.", InvoiceStatus.Draft, InvoiceStatus.Rejected);
        var termsDays = Vendor?.PaymentTermsDays ?? (DueDate.DayNumber - InvoiceDate.DayNumber);
        InvoiceDate = invoiceDate;
        DueDate = invoiceDate.AddDays(termsDays);
        Notes = notes;
        SetLines(lines, taxAmount);
        Status = InvoiceStatus.Draft;
        Record(InvoiceEventType.Updated, actor, null, now);
    }

    public void Submit(string actor, DateTimeOffset now)
    {
        EnsureStatus("invoice_not_submittable", "Only draft invoices can be submitted.", InvoiceStatus.Draft);
        if (_lines.Count == 0 || Total <= 0)
        {
            throw new DomainException("invoice_empty", "An invoice needs at least one line and a positive total before submission.");
        }

        Status = InvoiceStatus.Submitted;
        Record(InvoiceEventType.Submitted, actor, null, now);
    }

    public void Approve(string actor, IReadOnlyCollection<string> actorRoles, string? comment, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(actorRoles);
        EnsureStatus("invoice_not_pending", "Only submitted invoices can be approved.", InvoiceStatus.Submitted);
        EnsureSegregationOfDuties(actor);

        var isController = actorRoles.Contains(Roles.Controller);
        if (!isController && !actorRoles.Contains(Roles.Approver))
        {
            throw new DomainException("approval_forbidden", "Only approvers and controllers can approve invoices.");
        }

        if (!isController && Total > ApprovalPolicy.ApproverLimit)
        {
            throw new DomainException(
                "approval_limit_exceeded",
                $"Invoices above {ApprovalPolicy.ApproverLimit:C0} require a controller's approval.");
        }

        Status = InvoiceStatus.Approved;
        Record(InvoiceEventType.Approved, actor, comment, now);
    }

    public void Reject(string actor, string reason, DateTimeOffset now)
    {
        EnsureStatus("invoice_not_pending", "Only submitted invoices can be rejected.", InvoiceStatus.Submitted);
        EnsureSegregationOfDuties(actor);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("rejection_reason_required", "A rejection needs a reason the clerk can act on.");
        }

        Status = InvoiceStatus.Rejected;
        Record(InvoiceEventType.Rejected, actor, reason, now);
    }

    internal void Schedule(int paymentRunId, string actor, DateTimeOffset now)
    {
        EnsureStatus("invoice_not_payable", $"Invoice {InvoiceNumber} is not approved.", InvoiceStatus.Approved);
        Status = InvoiceStatus.Scheduled;
        PaymentRunId = paymentRunId;
        Record(InvoiceEventType.Scheduled, actor, $"Payment run #{paymentRunId}", now);
    }

    internal void Unschedule(string actor, DateTimeOffset now)
    {
        EnsureStatus("invoice_not_scheduled", $"Invoice {InvoiceNumber} is not scheduled.", InvoiceStatus.Scheduled);
        Status = InvoiceStatus.Approved;
        PaymentRunId = null;
        Record(InvoiceEventType.Unscheduled, actor, null, now);
    }

    internal void MarkPaid(string reference, string actor, DateTimeOffset now)
    {
        EnsureStatus("invoice_not_scheduled", $"Invoice {InvoiceNumber} is not scheduled.", InvoiceStatus.Scheduled);
        AmountPaid = Total;
        Status = InvoiceStatus.Paid;
        IsOverdue = false;
        Record(InvoiceEventType.Paid, actor, reference, now);
    }

    /// <summary>
    /// True when <paramref name="user"/> entered, edited or submitted this invoice. Requires <see cref="Events"/> to be loaded.
    /// </summary>
    public bool IsPreparedBy(string user) =>
        string.Equals(CreatedBy, user, StringComparison.OrdinalIgnoreCase)
        || _events.Any(e => PreparationEvents.Contains(e.Type) && string.Equals(e.Actor, user, StringComparison.OrdinalIgnoreCase));

    /// <summary>Re-evaluates the overdue flag. Returns true when the flag changed.</summary>
    public bool RefreshOverdue(DateOnly today, DateTimeOffset now)
    {
        var overdue = IsOpen && Status != InvoiceStatus.Draft && DueDate < today && Outstanding > 0;
        if (overdue == IsOverdue)
        {
            return false;
        }

        IsOverdue = overdue;
        if (overdue)
        {
            Record(InvoiceEventType.FlaggedOverdue, "system:overdue-job", $"Due {DueDate:yyyy-MM-dd}", now);
        }

        return true;
    }

    private void SetLines(IEnumerable<InvoiceLineInput> lines, decimal taxAmount)
    {
        ArgumentNullException.ThrowIfNull(lines);
        if (taxAmount < 0)
        {
            throw new DomainException("invalid_tax", "Tax cannot be negative.");
        }

        _lines.Clear();
        foreach (var line in lines)
        {
            _lines.Add(new InvoiceLine(line.Description.Trim(), line.GlAccount.Trim(), line.Quantity, line.UnitPrice));
        }

        Subtotal = _lines.Sum(l => l.LineTotal);
        TaxAmount = decimal.Round(taxAmount, 2, MidpointRounding.AwayFromZero);
        Total = Subtotal + TaxAmount;
    }

    private void EnsureSegregationOfDuties(string actor)
    {
        if (IsPreparedBy(actor))
        {
            throw new DomainException("segregation_of_duties", "The person who entered, edited or submitted an invoice cannot approve or reject it.");
        }
    }

    private void EnsureStatus(string code, string message, params InvoiceStatus[] allowed)
    {
        if (!allowed.Contains(Status))
        {
            throw new DomainException(code, $"{message} Current status: {Status}.");
        }
    }

    private void Record(InvoiceEventType type, string actor, string? comment, DateTimeOffset now) =>
        _events.Add(new InvoiceEvent(type, actor, comment, now));
}
