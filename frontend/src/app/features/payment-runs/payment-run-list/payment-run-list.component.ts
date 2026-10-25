import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PaymentRunsApi } from '@core/api';
import { AuthService } from '@core/auth';
import { PaymentRunSummary } from '@core/models';
import { LoadingComponent, StatusBadgeComponent } from '@shared/ui';

@Component({
  selector: 'lf-payment-run-list',
  imports: [CurrencyPipe, DatePipe, RouterLink, StatusBadgeComponent, LoadingComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './payment-run-list.component.html',
})
export class PaymentRunListComponent {
  readonly auth = inject(AuthService);
  readonly runs = signal<PaymentRunSummary[] | null>(null);

  constructor() {
    inject(PaymentRunsApi).list().subscribe(r => this.runs.set(r));
  }
}
