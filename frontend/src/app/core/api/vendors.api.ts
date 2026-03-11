import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Paged, SaveVendor, Vendor } from '../models';
import { toHttpParams } from './query-params';

export interface VendorQuery {
  search?: string;
  includeInactive?: boolean;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class VendorsApi {
  private readonly http = inject(HttpClient);

  list(query: VendorQuery): Observable<Paged<Vendor>> {
    return this.http.get<Paged<Vendor>>('/api/vendors', { params: toHttpParams(query) });
  }

  categories(): Observable<string[]> {
    return this.http.get<string[]>('/api/vendors/categories');
  }

  create(body: SaveVendor): Observable<Vendor> {
    return this.http.post<Vendor>('/api/vendors', body);
  }

  update(id: number, body: SaveVendor): Observable<Vendor> {
    return this.http.put<Vendor>(`/api/vendors/${id}`, body);
  }

  setActive(id: number, active: boolean): Observable<Vendor> {
    return this.http.post<Vendor>(`/api/vendors/${id}/${active ? 'activate' : 'deactivate'}`, {});
  }
}
