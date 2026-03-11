import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { InvoiceDetail, InvoiceQuery, InvoiceSummary, Paged, SaveInvoice } from '../models';
import { toHttpParams } from './query-params';

export type InvoiceAction = 'submit' | 'approve' | 'reject';

@Injectable({ providedIn: 'root' })
export class InvoicesApi {
  private readonly http = inject(HttpClient);

  list(query: InvoiceQuery): Observable<Paged<InvoiceSummary>> {
    return this.http.get<Paged<InvoiceSummary>>('/api/invoices', { params: toHttpParams(query) });
  }

  approvalQueue(): Observable<InvoiceSummary[]> {
    return this.http.get<InvoiceSummary[]>('/api/invoices/approval-queue');
  }

  get(id: number): Observable<InvoiceDetail> {
    return this.http.get<InvoiceDetail>(`/api/invoices/${id}`);
  }

  create(body: SaveInvoice): Observable<InvoiceDetail> {
    return this.http.post<InvoiceDetail>('/api/invoices', body);
  }

  update(id: number, body: SaveInvoice): Observable<InvoiceDetail> {
    return this.http.put<InvoiceDetail>(`/api/invoices/${id}`, body);
  }

  act(id: number, action: InvoiceAction, comment?: string): Observable<InvoiceDetail> {
    const body = action === 'submit' ? {} : { comment: comment ?? null };
    return this.http.post<InvoiceDetail>(`/api/invoices/${id}/${action}`, body);
  }
}
