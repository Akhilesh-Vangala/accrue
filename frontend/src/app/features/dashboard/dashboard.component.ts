import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DashboardApi } from '@core/api';
import { AuthService } from '@core/auth';
import { Dashboard } from '@core/models';
import { LoadingComponent } from '@shared/ui';

const BUCKET_COLORS = ['#10b981', '#f59e0b', '#f97316', '#ef4444', '#991b1b'];

@Component({
  selector: 'ac-dashboard',
  imports: [CurrencyPipe, DecimalPipe, DatePipe, RouterLink, LoadingComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {
  private readonly dashboardApi = inject(DashboardApi);
  readonly auth = inject(AuthService);
  readonly data = signal<Dashboard | null>(null);
  readonly colors = BUCKET_COLORS;

  readonly agingMax = computed(() => Math.max(1, ...(this.data()?.aging.map(a => a.amount) ?? [1])));
  readonly agingTotal = computed(() => this.data()?.aging.reduce((s, a) => s + a.amount, 0) ?? 0);

  /** Monthly spend bars scaled into a 600x170 viewBox. */
  readonly spendBars = computed(() => {
    const months = this.data()?.monthlySpend ?? [];
    const max = Math.max(1, ...months.map(m => m.amount));
    const slot = 600 / Math.max(1, months.length);
    return months.map((m, i) => {
      const h = (m.amount / max) * 150;
      return { x: i * slot + slot * 0.18, w: slot * 0.64, y: 160 - h, h, label: m.month, amount: m.amount, cx: i * slot + slot / 2 };
    });
  });

  /** Aging trend as two polylines (current vs overdue) in a 600x170 viewBox. */
  readonly trend = computed(() => {
    const pts = this.data()?.agingTrend ?? [];
    if (pts.length < 2) {
      return null;
    }
    const max = Math.max(1, ...pts.map(p => Math.max(p.current, p.overdue)));
    const x = (i: number) => (i / (pts.length - 1)) * 590 + 5;
    const y = (v: number) => 160 - (v / max) * 150;
    const line = (pick: (p: (typeof pts)[number]) => number) => pts.map((p, i) => `${x(i).toFixed(1)},${y(pick(p)).toFixed(1)}`).join(' ');
    return {
      current: line(p => p.current),
      overdue: line(p => p.overdue),
      first: pts[0].date,
      last: pts[pts.length - 1].date,
      latest: pts[pts.length - 1],
    };
  });

  constructor() {
    this.dashboardApi.get().subscribe(d => this.data.set(d));
  }
}
