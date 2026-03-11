using FluentValidation;
using LedgerFlow.Application.Abstractions;
using LedgerFlow.Application.Invoices;
using LedgerFlow.Domain.Common;
using LedgerFlow.Domain.Invoices;
using LedgerFlow.Domain.Operations;
using LedgerFlow.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace LedgerFlow.Application.Payments;

public sealed class PaymentRunService(
    ILedgerFlowDb db,
    ICurrentUser user,
    TimeProvider time,
    IValidator<CreatePaymentRunRequest> validator)
{
    public async Task<IReadOnlyList<PaymentRunSummaryDto>> ListAsync(CancellationToken ct) =>
        await Project(db.PaymentRuns.AsNoTracking().OrderByDescending(r => r.PaymentDate).ThenByDescending(r => r.Id)).ToListAsync(ct);

    public async Task<PaymentRunDetailDto> GetAsync(int id, CancellationToken ct)
    {
        var summary = await Project(db.PaymentRuns.AsNoTracking().Where(r => r.Id == id)).SingleOrDefaultAsync(ct)
            ?? throw NotFoundException.For<PaymentRun>(id);
        var payments = await db.Payments.AsNoTracking()
            .Where(p => p.PaymentRunId == id)
            .OrderBy(p => p.Id)
            .Select(p => new PaymentDto(p.Id, p.InvoiceId, p.Invoice!.InvoiceNumber, p.Invoice.Vendor!.Name, p.Invoice.DueDate, p.Amount, p.Reference))
            .ToListAsync(ct);
        return new PaymentRunDetailDto(summary, payments);
    }

    /// <summary>Approved invoices not yet in a payment run, soonest due first.</summary>
    public async Task<IReadOnlyList<InvoiceSummaryDto>> EligibleInvoicesAsync(CancellationToken ct) =>
        await InvoiceService.ProjectSummary(
                db.Invoices.AsNoTracking()
                    .Where(i => i.Status == InvoiceStatus.Approved && i.PaymentRunId == null)
                    .OrderBy(i => i.DueDate))
            .ToListAsync(ct);

    public async Task<PaymentRunDetailDto> CreateDraftAsync(CreatePaymentRunRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var id = await DraftAsync(request.Name.Trim(), request.PaymentDate, request.InvoiceIds, user.Email, ct);
        return await GetAsync(id, ct);
    }

    /// <summary>Creates a draft run holding the given approved invoices. Shared by the API and the auto payment-run job.</summary>
    public async Task<int> DraftAsync(string name, DateOnly paymentDate, IReadOnlyCollection<int> invoiceIds, string actor, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(invoiceIds);
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var invoices = await db.Invoices.Where(i => invoiceIds.Contains(i.Id)).ToListAsync(ct);
            var missing = invoiceIds.Except(invoices.Select(i => i.Id)).ToList();
            if (missing.Count > 0)
            {
                throw new NotFoundException($"Invoices not found: {string.Join(", ", missing)}.");
            }

            var run = new PaymentRun(name, paymentDate);
            db.PaymentRuns.Add(run);
            await db.SaveChangesAsync(ct);

            var now = time.GetUtcNow();
            foreach (var invoice in invoices.OrderBy(i => i.DueDate))
            {
                run.AddInvoice(invoice, actor, now);
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return run.Id;
        });
    }

    public async Task<PaymentRunDetailDto> ReleaseAsync(int id, CancellationToken ct)
    {
        var run = await LoadAsync(id, ct);
        run.Release(user.Email, user.Roles, time.GetUtcNow());
        db.Notifications.Add(new Notification(
            Roles.Clerk,
            $"Payment run released: {run.Name}",
            $"{run.Payments.Count} payments totalling {run.Total:C} were released by {user.Email}.",
            $"/payment-runs/{run.Id}",
            time.GetUtcNow()));
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<PaymentRunDetailDto> CancelAsync(int id, CancellationToken ct)
    {
        var run = await LoadAsync(id, ct);
        if (!user.Roles.Contains(Roles.Controller))
        {
            throw new DomainException("cancel_forbidden", "Only controllers can cancel a payment run.");
        }

        run.Cancel(user.Email, time.GetUtcNow());
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    private async Task<PaymentRun> LoadAsync(int id, CancellationToken ct) =>
        await db.PaymentRuns
            .Include(r => r.Payments).ThenInclude(p => p.Invoice)
            .SingleOrDefaultAsync(r => r.Id == id, ct)
        ?? throw NotFoundException.For<PaymentRun>(id);

    private static IQueryable<PaymentRunSummaryDto> Project(IQueryable<PaymentRun> runs) =>
        runs.Select(r => new PaymentRunSummaryDto(
            r.Id, r.Name, r.PaymentDate, r.Status, r.Total, r.Payments.Count, r.CreatedBy, r.ReleasedBy, r.ReleasedAt));
}
