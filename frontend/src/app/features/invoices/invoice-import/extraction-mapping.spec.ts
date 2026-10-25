import { InvoiceExtraction } from '@core/models';
import { blockingFindings, flaggedFields } from './extraction-mapping';

const extraction = (overrides: Partial<InvoiceExtraction> = {}): InvoiceExtraction => ({
  draft: { vendorId: 3, vendorName: 'Contoso', invoiceNumber: 'INV-1', invoiceDate: '2026-09-01', taxAmount: 0, statedTotal: 100, lines: [] },
  vendorMatchedBy: 'name',
  uncertainFields: [],
  findings: [],
  usage: { model: 'claude-opus-5-5', inputTokens: 1, outputTokens: 1, latencyMs: 1 },
  ...overrides,
});

describe('extraction mapping', () => {
  it('maps uncertain document fields onto form controls', () => {
    const flagged = flaggedFields(extraction({ uncertainFields: ['vendor_tax_id', 'invoice_date', 'line_items'] }));
    expect([...flagged].sort()).toEqual(['invoiceDate', 'lines', 'vendorId']);
  });

  it('flags fields named by errors and warnings but not by info findings', () => {
    const flagged = flaggedFields(extraction({
      findings: [
        { code: 'duplicate_invoice', severity: 'error', message: '', field: 'invoiceNumber' },
        { code: 'line_amount_mismatch', severity: 'warning', message: '', field: 'lines.1' },
        { code: 'gl_defaulted', severity: 'info', message: '', field: 'lines.0' },
      ],
    }));
    expect([...flagged].sort()).toEqual(['invoiceNumber', 'lines.1']);
  });

  it('counts only errors as blocking', () => {
    expect(blockingFindings([
      { code: 'a', severity: 'error', message: '', field: null },
      { code: 'b', severity: 'warning', message: '', field: null },
    ])).toBe(1);
  });
});
