export type InvoiceStatus = 'Draft' | 'Submitted' | 'Approved' | 'Rejected' | 'Scheduled' | 'Paid';

export interface InvoiceSummary {
  id: number;
  invoiceNumber: string;
  vendorId: number;
  vendorName: string;
  invoiceDate: string;
  dueDate: string;
  total: number;
  outstanding: number;
  status: InvoiceStatus;
  isOverdue: boolean;
  createdBy: string | null;
  paymentRunId: number | null;
}

export interface InvoiceLine {
  id?: number;
  description: string;
  glAccount: string;
  quantity: number;
  unitPrice: number;
  lineTotal?: number;
}

export interface InvoiceEvent {
  type: string;
  actor: string;
  comment: string | null;
  occurredAt: string;
}

export interface InvoiceDetail {
  summary: InvoiceSummary;
  subtotal: number;
  taxAmount: number;
  notes: string | null;
  lines: InvoiceLine[];
  events: InvoiceEvent[];
  allowedActions: string[];
  rowVersion: string;
}

export interface SaveInvoice {
  vendorId: number;
  invoiceNumber: string;
  invoiceDate: string;
  taxAmount: number;
  notes: string | null;
  lines: InvoiceLine[];
  rowVersion?: string | null;
}

export interface InvoiceQuery {
  status?: InvoiceStatus | '';
  vendorId?: number | null;
  overdue?: boolean | null;
  search?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}
