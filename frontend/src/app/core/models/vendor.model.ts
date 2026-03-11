export interface Vendor {
  id: number;
  name: string;
  taxId: string;
  email: string;
  category: string;
  paymentTermsDays: number;
  isActive: boolean;
  openBalance: number;
  openInvoices: number;
  rowVersion: string;
}

export interface SaveVendor {
  name: string;
  taxId: string;
  email: string;
  category: string;
  paymentTermsDays: number;
  rowVersion?: string | null;
}
