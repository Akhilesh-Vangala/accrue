import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreatePaymentRun, InvoiceSummary, PaymentRunDetail, PaymentRunSummary } from '../models';

export type PaymentRunAction = 'release' | 'cancel';

@Injectable({ providedIn: 'root' })
export class PaymentRunsApi {
  private readonly http = inject(HttpClient);

  list(): Observable<PaymentRunSummary[]> {
    return this.http.get<PaymentRunSummary[]>('/api/payment-runs');
  }

  get(id: number): Observable<PaymentRunDetail> {
    return this.http.get<PaymentRunDetail>(`/api/payment-runs/${id}`);
  }

  eligibleInvoices(): Observable<InvoiceSummary[]> {
    return this.http.get<InvoiceSummary[]>('/api/payment-runs/eligible-invoices');
  }

  create(body: CreatePaymentRun): Observable<PaymentRunDetail> {
    return this.http.post<PaymentRunDetail>('/api/payment-runs', body);
  }

  act(id: number, action: PaymentRunAction): Observable<PaymentRunDetail> {
    return this.http.post<PaymentRunDetail>(`/api/payment-runs/${id}/${action}`, {});
  }
}
