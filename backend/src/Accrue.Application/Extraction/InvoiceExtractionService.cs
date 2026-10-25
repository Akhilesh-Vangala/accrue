using Accrue.Application.Abstractions;
using Accrue.Domain.Common;
using Accrue.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace Accrue.Application.Extraction;

/// <summary>
/// Turns an uploaded invoice into a reviewed draft. The model only reads the document; vendor resolution,
/// GL coding fallbacks, arithmetic and duplicate checks are deterministic and run here, and nothing is saved.
/// </summary>
public sealed class InvoiceExtractionService(IAccrueDb db, IInvoiceDocumentReader reader, TimeProvider time)
{
    public const long MaxFileBytes = 10 * 1024 * 1024;

    public static readonly IReadOnlyList<string> AcceptedTypes = ["application/pdf", "image/png", "image/jpeg"];

    public ExtractionCapabilitiesDto Capabilities() =>
        new(reader.IsAvailable, reader.IsAvailable ? reader.Model : null, MaxFileBytes, AcceptedTypes);

    public async Task<InvoiceExtractionDto> ExtractAsync(InvoiceDocument document, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!reader.IsAvailable)
        {
            throw new DomainException("extraction_unavailable", "Document extraction is not configured on this server.");
        }

        if (!AcceptedTypes.Contains(document.MediaType))
        {
            throw new DomainException("unsupported_file_type", "Upload a PDF, PNG or JPEG invoice.");
        }

        if (document.Content.Length == 0 || document.Content.Length > MaxFileBytes)
        {
            throw new DomainException("file_too_large", $"Invoices must be between 1 byte and {MaxFileBytes / (1024 * 1024)} MB.");
        }

        var reading = await reader.ReadAsync(document, ct);
        var invoice = reading.Invoice;
        var findings = new List<ExtractionFinding>(ExtractionChecks.Run(invoice, DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime)));

        var vendors = await db.Vendors.AsNoTracking()
            .Select(v => new VendorCandidate(v.Id, v.Name, v.TaxId, v.Category, v.IsActive))
            .ToListAsync(ct);
        var match = VendorMatcher.Match(invoice, vendors);
        if (match is null)
        {
            findings.Add(new("vendor_not_found", "error", $"No vendor matches \"{invoice.VendorName}\"{(invoice.VendorTaxId is { } t ? $" (tax ID {t})" : string.Empty)}. Add the vendor or pick one.", "vendorId"));
        }
        else if (!match.Vendor.IsActive)
        {
            findings.Add(new("vendor_inactive", "error", $"{match.Vendor.Name} is inactive and cannot receive invoices.", "vendorId"));
        }

        if (match is not null && !string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
        {
            var number = invoice.InvoiceNumber.Trim();
            if (await db.Invoices.AnyAsync(i => i.VendorId == match.Vendor.Id && i.InvoiceNumber == number, ct))
            {
                findings.Add(new("duplicate_invoice", "error", $"Invoice {number} from {match.Vendor.Name} is already in Accrue. Possible duplicate payment.", "invoiceNumber"));
            }
        }

        var fallbackGl = ChartOfAccounts.DefaultFor(match?.Vendor.Category);
        var lines = new List<InvoiceLineInput>(invoice.Lines.Count);
        for (var i = 0; i < invoice.Lines.Count; i++)
        {
            var line = invoice.Lines[i];
            var gl = ChartOfAccounts.IsValid(line.GlAccount) ? line.GlAccount! : fallbackGl ?? string.Empty;
            if (!ChartOfAccounts.IsValid(line.GlAccount))
            {
                findings.Add(new("gl_defaulted", "info", $"Line {i + 1} was coded to {(gl.Length > 0 ? gl : "no account")} from the vendor's category; confirm the GL account.", $"lines.{i}"));
            }

            lines.Add(new InvoiceLineInput(line.Description, gl, line.Quantity, line.UnitPrice));
        }

        var draft = new ExtractedDraftDto(
            match?.Vendor.Id,
            invoice.VendorName,
            invoice.InvoiceNumber?.Trim(),
            invoice.InvoiceDate,
            invoice.TaxAmount ?? 0m,
            invoice.Total,
            lines);

        var ordered = findings.OrderBy(f => f.Severity switch { "error" => 0, "warning" => 1, _ => 2 }).ToList();
        return new InvoiceExtractionDto(draft, match?.MatchedBy, invoice.UncertainFields, ordered, reading.Usage);
    }
}
