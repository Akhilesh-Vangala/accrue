using FluentValidation;
using LedgerFlow.Application.Abstractions;
using LedgerFlow.Application.Common;
using LedgerFlow.Domain.Common;
using LedgerFlow.Domain.Invoices;
using LedgerFlow.Domain.Operations;
using LedgerFlow.Domain.Vendors;
using Microsoft.EntityFrameworkCore;

namespace LedgerFlow.Application.Invoices;

public sealed class InvoiceService(
    ILedgerFlowDb db,
    ICurrentUser user,
    TimeProvider time,
    IJobScheduler jobs,
    IValidator<SaveInvoiceRequest> validator)
{
    public async Task<PagedResult<InvoiceSummaryDto>> ListAsync(InvoiceQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var invoices = db.Invoices.AsNoTracking();
        if (query.Status is { } status)
        {
            invoices = invoices.Where(i => i.Status == status);
        }

        if (query.VendorId is { } vendorId)
        {
            invoices = invoices.Where(i => i.VendorId == vendorId);
        }

        if (query.Overdue is { } overdue)
        {
            invoices = invoices.Where(i => i.IsOverdue == overdue);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            invoices = invoices.Where(i => i.InvoiceNumber.Contains(term) || i.Vendor!.Name.Contains(term));
        }

        invoices = query.Sort switch
        {
            "total" => invoices.OrderBy(i => i.Total),
            "-total" => invoices.OrderByDescending(i => i.Total),
            "dueDate" => invoices.OrderBy(i => i.DueDate),
            "-dueDate" => invoices.OrderByDescending(i => i.DueDate),
            "vendor" => invoices.OrderBy(i => i.Vendor!.Name),
            "invoiceDate" => invoices.OrderBy(i => i.InvoiceDate),
            _ => invoices.OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.Id),
        };

        return await ProjectSummary(invoices).ToPagedAsync(query.Page, query.PageSize, ct);
    }

    /// <summary>Submitted invoices the current user is allowed to decide on.</summary>
    public async Task<IReadOnlyList<InvoiceSummaryDto>> ApprovalQueueAsync(CancellationToken ct)
    {
        var isController = user.Roles.Contains(Roles.Controller);
        var queue = db.Invoices.AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Submitted && i.CreatedBy != user.Email);
        if (!isController)
        {
            queue = queue.Where(i => i.Total <= ApprovalPolicy.ApproverLimit);
        }

        return await ProjectSummary(queue.OrderBy(i => i.DueDate)).ToListAsync(ct);
    }

    public async Task<InvoiceDetailDto> GetAsync(int id, CancellationToken ct)
    {
        var invoice = await db.Invoices.AsNoTracking()
            .Include(i => i.Vendor)
            .Include(i => i.Lines)
            .Include(i => i.Events)
            .SingleOrDefaultAsync(i => i.Id == id, ct)
            ?? throw NotFoundException.For<Invoice>(id);
        return ToDetail(invoice);
    }

    public async Task<InvoiceDetailDto> CreateAsync(SaveInvoiceRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var vendor = await db.Vendors.SingleOrDefaultAsync(v => v.Id == request.VendorId, ct)
            ?? throw NotFoundException.For<Vendor>(request.VendorId);
        await EnsureNotDuplicateAsync(request.VendorId, request.InvoiceNumber, null, ct);

        var invoice = new Invoice(vendor, request.InvoiceNumber, request.InvoiceDate, request.TaxAmount, request.Lines, user.Email, time.GetUtcNow(), request.Notes);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);
        return await GetAsync(invoice.Id, ct);
    }

    public async Task<InvoiceDetailDto> UpdateAsync(int id, SaveInvoiceRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var invoice = await LoadAsync(id, ct);
        ConcurrencyToken.Apply(db, invoice, request.RowVersion);
        if (invoice.VendorId != request.VendorId)
        {
            throw new DomainException("vendor_immutable", "An invoice cannot be moved to a different vendor; reject it and enter a new one.");
        }

        await EnsureNotDuplicateAsync(request.VendorId, request.InvoiceNumber, id, ct);
        invoice.UpdateDraft(request.InvoiceDate, request.TaxAmount, request.Lines, request.Notes, user.Email, time.GetUtcNow());
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<InvoiceDetailDto> SubmitAsync(int id, CancellationToken ct)
    {
        var invoice = await LoadAsync(id, ct);
        invoice.Submit(user.Email, time.GetUtcNow());
        var audience = invoice.Total > ApprovalPolicy.ApproverLimit ? Roles.Controller : Roles.Approver;
        db.Notifications.Add(new Notification(
            audience,
            $"Approval needed: {invoice.InvoiceNumber}",
            $"{invoice.Vendor!.Name} invoice for {invoice.Total:C} was submitted by {user.Email}.",
            $"/invoices/{invoice.Id}",
            time.GetUtcNow()));
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<InvoiceDetailDto> ApproveAsync(int id, DecisionRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var invoice = await LoadAsync(id, ct);
        invoice.Approve(user.Email, user.Roles, request.Comment, time.GetUtcNow());
        await db.SaveChangesAsync(ct);
        jobs.EnqueueInvoiceDecisionNotification(id);
        return await GetAsync(id, ct);
    }

    public async Task<InvoiceDetailDto> RejectAsync(int id, DecisionRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var invoice = await LoadAsync(id, ct);
        invoice.Reject(user.Email, request.Comment ?? string.Empty, time.GetUtcNow());
        await db.SaveChangesAsync(ct);
        jobs.EnqueueInvoiceDecisionNotification(id);
        return await GetAsync(id, ct);
    }

    private async Task<Invoice> LoadAsync(int id, CancellationToken ct) =>
        await db.Invoices.Include(i => i.Vendor).Include(i => i.Lines).SingleOrDefaultAsync(i => i.Id == id, ct)
        ?? throw NotFoundException.For<Invoice>(id);

    private async Task EnsureNotDuplicateAsync(int vendorId, string invoiceNumber, int? exceptId, CancellationToken ct)
    {
        var number = invoiceNumber.Trim();
        if (await db.Invoices.AnyAsync(i => i.VendorId == vendorId && i.InvoiceNumber == number && i.Id != exceptId, ct))
        {
            throw new DomainException("duplicate_invoice", $"Invoice {number} already exists for this vendor. Possible duplicate payment.");
        }
    }

    private List<string> AllowedActions(Invoice invoice)
    {
        var actions = new List<string>();
        var isOwner = string.Equals(invoice.CreatedBy, user.Email, StringComparison.OrdinalIgnoreCase);
        var isController = user.Roles.Contains(Roles.Controller);
        var canDecide = !isOwner && (isController || (user.Roles.Contains(Roles.Approver) && invoice.Total <= ApprovalPolicy.ApproverLimit));

        switch (invoice.Status)
        {
            case InvoiceStatus.Draft:
                actions.Add("edit");
                actions.Add("submit");
                break;
            case InvoiceStatus.Rejected:
                actions.Add("edit");
                break;
            case InvoiceStatus.Submitted when canDecide:
                actions.Add("approve");
                actions.Add("reject");
                break;
            default:
                break;
        }

        return actions;
    }

    private InvoiceDetailDto ToDetail(Invoice i) => new(
        new InvoiceSummaryDto(i.Id, i.InvoiceNumber, i.VendorId, i.Vendor!.Name, i.InvoiceDate, i.DueDate, i.Total, i.Outstanding, i.Status, i.IsOverdue, i.CreatedBy, i.PaymentRunId),
        i.Subtotal,
        i.TaxAmount,
        i.Notes,
        [.. i.Lines.OrderBy(l => l.Id).Select(l => new InvoiceLineDto(l.Id, l.Description, l.GlAccount, l.Quantity, l.UnitPrice, l.LineTotal))],
        [.. i.Events.OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Id).Select(e => new InvoiceEventDto(e.Type, e.Actor, e.Comment, e.OccurredAt))],
        AllowedActions(i),
        Convert.ToBase64String(i.RowVersion));

    internal static IQueryable<InvoiceSummaryDto> ProjectSummary(IQueryable<Invoice> invoices) =>
        invoices.Select(i => new InvoiceSummaryDto(
            i.Id,
            i.InvoiceNumber,
            i.VendorId,
            i.Vendor!.Name,
            i.InvoiceDate,
            i.DueDate,
            i.Total,
            i.Total - i.AmountPaid,
            i.Status,
            i.IsOverdue,
            i.CreatedBy,
            i.PaymentRunId));
}
