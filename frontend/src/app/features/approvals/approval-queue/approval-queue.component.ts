import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { InvoicesApi } from '@core/api';
import { AuthService } from '@core/auth';
import { InvoiceSummary } from '@core/models';
import { LoadingComponent, StatusBadgeComponent } from '@shared/ui';

@Component({
  selector: 'ac-approval-queue',
  imports: [CurrencyPipe, DatePipe, RouterLink, LoadingComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './approval-queue.component.html',
})
export class ApprovalQueueComponent {
  readonly auth = inject(AuthService);
  readonly queue = signal<InvoiceSummary[] | null>(null);
  readonly total = computed(() => this.queue()?.reduce((s, i) => s + i.total, 0) ?? 0);

  constructor() {
    inject(InvoicesApi).approvalQueue().subscribe(q => this.queue.set(q));
  }
}
