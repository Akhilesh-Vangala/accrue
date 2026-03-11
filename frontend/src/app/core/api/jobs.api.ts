import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { RecurringJob, TriggeredJob } from '../models';

@Injectable({ providedIn: 'root' })
export class JobsApi {
  private readonly http = inject(HttpClient);

  list(): Observable<RecurringJob[]> {
    return this.http.get<RecurringJob[]>('/api/jobs');
  }

  trigger(id: string): Observable<TriggeredJob> {
    return this.http.post<TriggeredJob>(`/api/jobs/${id}/trigger`, {});
  }
}
