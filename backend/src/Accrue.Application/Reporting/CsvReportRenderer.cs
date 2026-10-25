using System.Globalization;
using System.Text;
using Accrue.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Accrue.Application.Reporting;

/// <summary>Renders the report datasets (the same SQL views Telerik binds to) as CSV. Always available.</summary>
public sealed class CsvReportRenderer(IAccrueDb db, TimeProvider time) : IReportRenderer
{
    public string Engine => "CSV";

    public bool SupportsPdf => false;

    public async Task<RenderedReport> RenderAsync(string reportName, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var sb = new StringBuilder();
        switch (reportName)
        {
            case ReportNames.ApAging:
                sb.AppendLine("Vendor,Invoice,Invoice Date,Due Date,Status,Days Past Due,Bucket,Outstanding");
                foreach (var r in await db.ApAging.OrderBy(r => r.BucketOrder).ThenBy(r => r.VendorName).ThenBy(r => r.DueDate).ToListAsync(cancellationToken))
                {
                    AppendRow(sb, r.VendorName, r.InvoiceNumber, r.InvoiceDate, r.DueDate, r.Status, r.DaysPastDue, r.Bucket, r.Outstanding);
                }

                break;
            case ReportNames.VendorSpend:
                var from = (DateOnly)parameters["from"]!;
                var to = (DateOnly)parameters["to"]!;
                sb.AppendLine("Month,Vendor,Category,Invoices,Amount");
                var spend = await db.VendorSpend
                    .Where(s => s.PeriodStart >= new DateOnly(from.Year, from.Month, 1) && s.PeriodStart <= to)
                    .OrderBy(s => s.PeriodStart).ThenByDescending(s => s.TotalAmount)
                    .ToListAsync(cancellationToken);
                foreach (var r in spend)
                {
                    AppendRow(sb, r.PeriodStart.ToString("yyyy-MM", CultureInfo.InvariantCulture), r.VendorName, r.Category, r.InvoiceCount, r.TotalAmount);
                }

                break;
            case ReportNames.PaymentRemittance:
                var runId = (int)parameters["paymentRunId"]!;
                sb.AppendLine("Run,Payment Date,Status,Vendor,Vendor Email,Invoice,Invoice Date,Amount,Reference");
                var remittance = await db.PaymentRemittance
                    .Where(r => r.PaymentRunId == runId)
                    .OrderBy(r => r.VendorName).ThenBy(r => r.InvoiceNumber)
                    .ToListAsync(cancellationToken);
                if (remittance.Count == 0)
                {
                    throw new NotFoundException($"Payment run {runId} has no payments.");
                }

                foreach (var r in remittance)
                {
                    AppendRow(sb, r.RunName, r.PaymentDate, r.RunStatus, r.VendorName, r.VendorEmail, r.InvoiceNumber, r.InvoiceDate, r.Amount, r.Reference);
                }

                break;
            default:
                throw new NotFoundException($"Report '{reportName}' does not exist.");
        }

        var stamp = time.GetUtcNow().ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        return new RenderedReport($"{reportName}-{stamp}.csv", "text/csv", Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray());
    }

    private static void AppendRow(StringBuilder sb, params object?[] values)
    {
        sb.AppendLine(string.Join(',', values.Select(Escape)));
    }

    private static string Escape(object? value)
    {
        var text = value switch
        {
            null => string.Empty,
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

        // Leading =, +, -, @ would be evaluated as formulas when opened in Excel.
        if (text.Length > 0 && "=+-@".Contains(text[0], StringComparison.Ordinal) && value is string)
        {
            text = "'" + text;
        }

        return text.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : text;
    }
}
