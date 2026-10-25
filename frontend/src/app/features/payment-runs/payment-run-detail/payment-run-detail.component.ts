import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PaymentRunsApi, ReportsApi } from '@core/api';
import { AuthService } from '@core/auth';
import { describeError } from '@core/http';
import { PaymentRunDetail } from '@core/models';
import { ToastService } from '@core/toast';
import { LoadingComponent, StatusBadgeComponent } from '@shared/ui';

@Component({
  selector: 'ac-payment-run-detail',
  imports: [CurrencyPipe, DatePipe, RouterLink, StatusBadgeComponent, LoadingComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './payment-run-detail.component.html',
})
export class PaymentRunDetailComponent implements OnInit {
  readonly id = input.required<string>();
  readonly auth = inject(AuthService);
  private readonly paymentRunsApi = inject(PaymentRunsApi);
  private readonly reportsApi = inject(ReportsApi);
  private readonly toast = inject(ToastService);
  readonly run = signal<PaymentRunDetail | null>(null);
  readonly busy = signal(false);

  ngOnInit(): void {
    this.paymentRunsApi.get(Number(this.id())).subscribe(r => this.run.set(r));
  }

  remittanceCsv(): string {
    return this.reportsApi.downloadUrl('PaymentRemittance', 'csv', { paymentRunId: this.id() });
  }

  act(action: 'release' | 'cancel'): void {
    const verb = action === 'release' ? 'Release this run and mark its invoices paid?' : 'Cancel this run and return its invoices to Approved?';
    if (!confirm(verb)) {
      return;
    }
    this.busy.set(true);
    this.paymentRunsApi.act(Number(this.id()), action).subscribe({
      next: r => {
        this.run.set(r);
        this.busy.set(false);
        this.toast.success(action === 'release' ? `Released ${r.payments.length} payments.` : 'Payment run cancelled.');
      },
      error: err => {
        this.busy.set(false);
        this.toast.error(describeError(err));
      },
    });
  }
}
