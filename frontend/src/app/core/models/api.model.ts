export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/** RFC 9457 problem details as produced by the API's exception handler. */
export interface Problem {
  status: number;
  title: string;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
}
