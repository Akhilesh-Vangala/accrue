import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ReportArchive, ReportCapabilities } from '../models';
import { toHttpParams } from './query-params';

export type ReportFormat = 'csv' | 'pdf';

@Injectable({ providedIn: 'root' })
export class ReportsApi {
  private readonly http = inject(HttpClient);

  capabilities(): Observable<ReportCapabilities> {
    return this.http.get<ReportCapabilities>('/api/reports/capabilities');
  }

  archive(): Observable<ReportArchive[]> {
    return this.http.get<ReportArchive[]>('/api/reports/archive');
  }

  /** Same-origin download URL; the auth cookie authorizes the request. */
  downloadUrl(name: string, format: ReportFormat, query: Record<string, string | number | null | undefined> = {}): string {
    const qs = toHttpParams(query).toString();
    return `/api/reports/${name}.${format}${qs ? `?${qs}` : ''}`;
  }

  archiveUrl(id: number): string {
    return `/api/reports/archive/${id}`;
  }
}
