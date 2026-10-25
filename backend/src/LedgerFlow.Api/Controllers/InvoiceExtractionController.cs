using LedgerFlow.Application.Extraction;
using LedgerFlow.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LedgerFlow.Api.Controllers;

/// <summary>Reads an uploaded invoice into a draft for review. Nothing is saved until the user submits the form.</summary>
[ApiController]
[Route("api/invoices/extraction")]
[Authorize(Roles = $"{Roles.Clerk},{Roles.Controller}")]
public sealed class InvoiceExtractionController(InvoiceExtractionService extraction) : ControllerBase
{
    [HttpGet]
    public ExtractionCapabilitiesDto Capabilities() => extraction.Capabilities();

    [HttpPost]
    [RequestSizeLimit(InvoiceExtractionService.MaxFileBytes + 64 * 1024)]
    [EnableRateLimiting("extraction")]
    public async Task<InvoiceExtractionDto> Extract(IFormFile file, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (file.Length > InvoiceExtractionService.MaxFileBytes)
        {
            throw new DomainException("file_too_large", "Invoices must be 10 MB or smaller.");
        }

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, ct);
        return await extraction.ExtractAsync(new InvoiceDocument(file.FileName, file.ContentType, buffer.ToArray()), ct);
    }
}
