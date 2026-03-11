export type PaymentRunStatus = 'Draft' | 'Released' | 'Cancelled';

export interface PaymentRunSummary {
  id: number;
  name: string;
  paymentDate: string;
  status: PaymentRunStatus;
  total: number;
  paymentCount: number;
  createdBy: string | null;
  releasedBy: string | null;
  releasedAt: string | null;
}

export interface Payment {
  id: number;
  invoiceId: number;
  invoiceNumber: string;
  vendorName: string;
  dueDate: string;
  amount: number;
  reference: string | null;
}

export interface PaymentRunDetail {
  summary: PaymentRunSummary;
  payments: Payment[];
}

export interface CreatePaymentRun {
  name: string;
  paymentDate: string;
  invoiceIds: number[];
}
