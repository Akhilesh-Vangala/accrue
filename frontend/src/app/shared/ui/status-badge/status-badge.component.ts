import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { InvoiceStatus, PaymentRunStatus } from '@core/models';

const STATUS_CLASS: Record<InvoiceStatus | PaymentRunStatus, string> = {
  Draft: 'text-bg-light border',
  Submitted: 'text-bg-warning',
  Approved: 'text-bg-info',
  Rejected: 'text-bg-danger',
  Scheduled: 'text-bg-primary',
  Paid: 'text-bg-success',
  Released: 'text-bg-success',
  Cancelled: 'text-bg-secondary',
};

/** Colored status pill with an optional overdue marker. */
@Component({
  selector: 'lf-status',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="badge badge-status {{ css() }}">{{ status() }}</span>
    @if (overdue()) {
      <span class="badge badge-status text-bg-danger ms-1"><i class="bi bi-alarm"></i> Overdue</span>
    }
  `,
})
export class StatusBadgeComponent {
  readonly status = input.required<InvoiceStatus | PaymentRunStatus>();
  readonly overdue = input(false);
  readonly css = computed(() => STATUS_CLASS[this.status()] ?? 'text-bg-light');
}
