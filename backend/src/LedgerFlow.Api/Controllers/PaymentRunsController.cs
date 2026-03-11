using LedgerFlow.Application.Invoices;
using LedgerFlow.Application.Payments;
using LedgerFlow.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerFlow.Api.Controllers;

[ApiController]
[Route("api/payment-runs")]
[Authorize]
public sealed class PaymentRunsController(PaymentRunService runs) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<PaymentRunSummaryDto>> List(CancellationToken ct) => runs.ListAsync(ct);

    [HttpGet("eligible-invoices")]
    [Authorize(Roles = $"{Roles.Clerk},{Roles.Controller}")]
    public Task<IReadOnlyList<InvoiceSummaryDto>> Eligible(CancellationToken ct) => runs.EligibleInvoicesAsync(ct);

    [HttpGet("{id:int}")]
    public Task<PaymentRunDetailDto> Get(int id, CancellationToken ct) => runs.GetAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = $"{Roles.Clerk},{Roles.Controller}")]
    public async Task<ActionResult<PaymentRunDetailDto>> Create(CreatePaymentRunRequest request, CancellationToken ct)
    {
        var run = await runs.CreateDraftAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = run.Summary.Id }, run);
    }

    [HttpPost("{id:int}/release")]
    [Authorize(Roles = Roles.Controller)]
    public Task<PaymentRunDetailDto> Release(int id, CancellationToken ct) => runs.ReleaseAsync(id, ct);

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = Roles.Controller)]
    public Task<PaymentRunDetailDto> Cancel(int id, CancellationToken ct) => runs.CancelAsync(id, ct);
}
