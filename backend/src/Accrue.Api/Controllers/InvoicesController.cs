using Accrue.Application.Common;
using Accrue.Application.Invoices;
using Accrue.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accrue.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public sealed class InvoicesController(InvoiceService invoices) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<InvoiceSummaryDto>> List([FromQuery] InvoiceQuery query, CancellationToken ct) => invoices.ListAsync(query, ct);

    [HttpGet("approval-queue")]
    [Authorize(Roles = Roles.ApproversAndControllers)]
    public Task<IReadOnlyList<InvoiceSummaryDto>> ApprovalQueue(CancellationToken ct) => invoices.ApprovalQueueAsync(ct);

    [HttpGet("{id:int}")]
    public Task<InvoiceDetailDto> Get(int id, CancellationToken ct) => invoices.GetAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = $"{Roles.Clerk},{Roles.Controller}")]
    public async Task<ActionResult<InvoiceDetailDto>> Create(SaveInvoiceRequest request, CancellationToken ct)
    {
        var invoice = await invoices.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = invoice.Summary.Id }, invoice);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Clerk},{Roles.Controller}")]
    public Task<InvoiceDetailDto> Update(int id, SaveInvoiceRequest request, CancellationToken ct) => invoices.UpdateAsync(id, request, ct);

    [HttpPost("{id:int}/submit")]
    [Authorize(Roles = $"{Roles.Clerk},{Roles.Controller}")]
    public Task<InvoiceDetailDto> Submit(int id, CancellationToken ct) => invoices.SubmitAsync(id, ct);

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = Roles.ApproversAndControllers)]
    public Task<InvoiceDetailDto> Approve(int id, DecisionRequest request, CancellationToken ct) => invoices.ApproveAsync(id, request, ct);

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = Roles.ApproversAndControllers)]
    public Task<InvoiceDetailDto> Reject(int id, DecisionRequest request, CancellationToken ct) => invoices.RejectAsync(id, request, ct);
}
