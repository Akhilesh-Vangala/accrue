import { HttpParams } from '@angular/common/http';

/** Converts a query object to HttpParams, skipping null, undefined and empty values. */
export function toHttpParams(source: object): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(source)) {
    if (value !== null && value !== undefined && value !== '') {
      params = params.set(key, String(value));
    }
  }
  return params;
}
