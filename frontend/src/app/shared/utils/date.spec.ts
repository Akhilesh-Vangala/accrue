import { isoDate } from './date';

describe('isoDate', () => {
  it('formats a local date without timezone drift', () => {
    expect(isoDate(new Date(2026, 0, 5))).toBe('2026-01-05');
  });
});
