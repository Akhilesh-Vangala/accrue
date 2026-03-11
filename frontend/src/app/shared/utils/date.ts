/** ISO yyyy-MM-dd for <input type="date"> and API DateOnly fields, in local time. */
export function isoDate(date: Date): string {
  const offset = date.getTimezoneOffset() * 60_000;
  return new Date(date.getTime() - offset).toISOString().slice(0, 10);
}
