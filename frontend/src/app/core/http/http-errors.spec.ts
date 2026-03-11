import { HttpErrorResponse } from '@angular/common/http';
import { describeError, fieldErrors } from './http-errors';

const problem = (status: number, body: object | null) => new HttpErrorResponse({ status, error: body });

describe('describeError', () => {
  it('joins validation messages', () => {
    const error = problem(400, { status: 400, title: 'Validation failed', errors: { lines: ['Add at least one line.'], taxId: ['Bad tax id.'] } });
    expect(describeError(error)).toBe('Add at least one line. Bad tax id.');
  });

  it('prefers the problem detail for business rule conflicts', () => {
    const error = problem(409, { status: 409, title: 'Business rule violated', detail: 'Invoices above $10,000 require a controller.' });
    expect(describeError(error)).toBe('Invoices above $10,000 require a controller.');
  });

  it('explains network failures and forbidden roles', () => {
    expect(describeError(problem(0, null))).toContain('Cannot reach the server');
    expect(describeError(problem(403, null))).toBe('Your role is not allowed to do that.');
  });

  it('falls back for unknown errors', () => {
    expect(describeError(problem(502, null))).toBe('Request failed (502).');
    expect(describeError(new Error('boom'))).toBe('Something went wrong.');
  });
});

describe('fieldErrors', () => {
  it('returns field messages keyed by property', () => {
    expect(fieldErrors(problem(400, { errors: { name: ['Required.'] } }))).toEqual({ name: ['Required.'] });
    expect(fieldErrors(new Error('x'))).toEqual({});
  });
});
