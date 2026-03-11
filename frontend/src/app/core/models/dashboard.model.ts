import { InvoiceStatus } from './invoice.model';

export interface Dashboard {
  kpis: {
    openPayables: number;
    openInvoices: number;
    overdueAmount: number;
    overdueInvoices: number;
    pendingApprovals: number;
    readyToPay: number;
    paidLast30Days: number;
    averageApprovalHours: number | null;
  };
  aging: { bucket: string; order: number; invoiceCount: number; amount: number }[];
  topVendors: { vendorId: number; vendorName: string; openBalance: number; openInvoices: number }[];
  monthlySpend: { month: string; amount: number; invoiceCount: number }[];
  agingTrend: { date: string; current: number; overdue: number }[];
  statusCounts: { status: InvoiceStatus; count: number }[];
}
