import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { InvoicesApi } from '@core/api';
import { describeError } from '@core/http';
import { InvoiceDetail } from '@core/models';
import { ToastService } from '@core/toast';
import { LoadingComponent, StatusBadgeComponent } from '@shared/ui';

const EVENT_ICON: Record<string, string> = {
  Created: 'bi-plus-circle',
  Updated: 'bi-pencil',
  Submitted: 'bi-send',
  Approved: 'bi-check-circle-fill text-success',
  Rejected: 'bi-x-circle-fill text-danger',
  Scheduled: 'bi-calendar-check text-primary',
  Unscheduled: 'bi-calendar-x',
  Paid: 'bi-cash-coin text-success',
  FlaggedOverdue: 'bi-alarm text-danger',
};

@Component({
  selector: 'ac-invoice-detail',
  imports: [CurrencyPipe, DatePipe, FormsModule, RouterLink, StatusBadgeComponent, LoadingComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './invoice-detail.component.html',
  styleUrl: './invoice-detail.component.scss',
})
export class InvoiceDetailComponent implements OnInit {
  readonly id = input.required<string>();

  private readonly invoicesApi = inject(InvoicesApi);
  private readonly toast = inject(ToastService);

  readonly invoice = signal<InvoiceDetail | null>(null);
  readonly busy = signal(false);
  readonly decision = signal<'approve' | 'reject' | null>(null);
  comment = '';

  ngOnInit(): void {
    this.invoicesApi.get(Number(this.id())).subscribe(i => this.invoice.set(i));
  }

  icon(type: string): string {
    return EVENT_ICON[type] ?? 'bi-dot';
  }

  can(action: string): boolean {
    return this.invoice()?.allowedActions.includes(action) ?? false;
  }

  openDecision(kind: 'approve' | 'reject'): void {
    this.comment = '';
    this.decision.set(kind);
  }

  run(action: 'submit' | 'approve' | 'reject'): void {
    const current = this.invoice();
    if (!current) {
      return;
    }
    this.busy.set(true);
    this.invoicesApi.act(current.summary.id, action, this.comment.trim() || undefined).subscribe({
      next: updated => {
        this.invoice.set(updated);
        this.busy.set(false);
        this.decision.set(null);
        const verb = { submit: 'submitted for approval', approve: 'approved', reject: 'rejected' }[action];
        this.toast.success(`Invoice ${updated.summary.invoiceNumber} ${verb}.`);
      },
      error: err => {
        this.busy.set(false);
        this.toast.error(describeError(err));
      },
    });
  }
}
