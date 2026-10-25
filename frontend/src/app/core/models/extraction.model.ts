export type FindingSeverity = 'error' | 'warning' | 'info';

export interface ExtractionCapabilities {
  enabled: boolean;
  model: string | null;
  maxFileBytes: number;
  acceptedTypes: string[];
}

export interface ExtractionFinding {
  code: string;
  severity: FindingSeverity;
  message: string;
  /** Form field the finding applies to, e.g. `invoiceNumber` or `lines.2`. */
  field: string | null;
}

export interface ExtractedDraft {
  vendorId: number | null;
  vendorName: string | null;
  invoiceNumber: string | null;
  invoiceDate: string | null;
  taxAmount: number;
  statedTotal: number | null;
  lines: { description: string; glAccount: string; quantity: number; unitPrice: number }[];
}

export interface InvoiceExtraction {
  draft: ExtractedDraft;
  vendorMatchedBy: 'tax_id' | 'name' | null;
  uncertainFields: string[];
  findings: ExtractionFinding[];
  usage: { model: string; inputTokens: number; outputTokens: number; latencyMs: number };
}
