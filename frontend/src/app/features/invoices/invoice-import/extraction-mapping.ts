import { ExtractionFinding, InvoiceExtraction } from '@core/models';

/** Maps the extractor's field names (snake_case, as printed on the document) to invoice form controls. */
const UNCERTAIN_TO_FORM: Record<string, string> = {
  vendor_name: 'vendorId',
  vendor_tax_id: 'vendorId',
  invoice_number: 'invoiceNumber',
  invoice_date: 'invoiceDate',
  tax_amount: 'taxAmount',
  subtotal: 'lines',
  total: 'taxAmount',
  line_items: 'lines',
};

/**
 * Form fields that need a reviewer's eye: anything the model marked uncertain, plus anything an error or
 * warning finding points at. A line-level finding (`lines.2`) flags that line only.
 */
export function flaggedFields(extraction: InvoiceExtraction): Set<string> {
  const flagged = new Set<string>();
  extraction.uncertainFields.forEach(f => UNCERTAIN_TO_FORM[f] && flagged.add(UNCERTAIN_TO_FORM[f]));
  extraction.findings
    .filter(f => f.severity !== 'info' && f.field)
    .forEach(f => flagged.add(f.field!));
  return flagged;
}

export function blockingFindings(findings: ExtractionFinding[]): number {
  return findings.filter(f => f.severity === 'error').length;
}
