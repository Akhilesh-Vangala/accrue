import { HttpErrorResponse } from '@angular/common/http';
import { Problem } from '../models';

/** Turns an API error into one readable sentence for toasts and inline alerts. */
export function describeError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const problem = error.error as Problem | null;
    if (problem?.errors) {
      return Object.values(problem.errors).flat().join(' ');
    }
    if (problem?.detail) {
      return problem.detail;
    }
    if (error.status === 0) {
      return 'Cannot reach the server. Check that the API is running.';
    }
    if (error.status === 403) {
      return 'Your role is not allowed to do that.';
    }
    return problem?.title ?? `Request failed (${error.status}).`;
  }
  return 'Something went wrong.';
}

/** Field-level validation messages keyed by camelCase property name. */
export function fieldErrors(error: unknown): Record<string, string[]> {
  return error instanceof HttpErrorResponse ? ((error.error as Problem | null)?.errors ?? {}) : {};
}
