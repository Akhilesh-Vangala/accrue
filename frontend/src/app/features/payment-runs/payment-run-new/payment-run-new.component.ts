import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PaymentRunsApi } from '@core/api';
import { describeError } from '@core/http';
import { InvoiceSummary } from '@core/models';
import { ToastService } from '@core/toast';
import { LoadingComponent } from '@shared/ui';
import { isoDate } from '@shared/utils';

@Component({
  selector: 'lf-payment-run-new',
  imports: [CurrencyPipe, DatePipe, FormsModule, RouterLink, LoadingComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './payment-run-new.component.html',
})
export class PaymentRunNewComponent {
  private readonly paymentRunsApi = inject(PaymentRunsApi);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly today = isoDate(new Date());
  readonly eligible = signal<InvoiceSummary[] | null>(null);
  readonly selected = signal(new Set<number>());
  readonly busy = signal(false);
  name = `Payment run ${this.today}`;
  paymentDate = isoDate(new Date(Date.now() + 2 * 86_400_000));

  readonly selectedTotal = computed(() =>
    (this.eligible() ?? []).filter(i => this.selected().has(i.id)).reduce((s, i) => s + i.outstanding, 0),
  );
  readonly allSelected = computed(() => {
    const list = this.eligible() ?? [];
    return list.length > 0 && list.every(i => this.selected().has(i.id));
  });

  constructor() {
    this.paymentRunsApi.eligibleInvoices().subscribe(list => this.eligible.set(list));
  }

  toggle(id: number): void {
    const next = new Set(this.selected());
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    this.selected.set(next);
  }

  toggleAll(): void {
    this.selected.set(this.allSelected() ? new Set() : new Set((this.eligible() ?? []).map(i => i.id)));
  }

  selectDueWithin(days: number): void {
    const cutoff = isoDate(new Date(Date.now() + days * 86_400_000));
    this.selected.set(new Set((this.eligible() ?? []).filter(i => i.dueDate <= cutoff).map(i => i.id)));
  }

  create(): void {
    this.busy.set(true);
    this.paymentRunsApi.create({ name: this.name.trim(), paymentDate: this.paymentDate, invoiceIds: [...this.selected()] }).subscribe({
      next: run => {
        this.toast.success(`Draft run created with ${run.payments.length} payments.`);
        void this.router.navigate(['/payment-runs', run.summary.id]);
      },
      error: err => {
        this.busy.set(false);
        this.toast.error(describeError(err));
      },
    });
  }
}
