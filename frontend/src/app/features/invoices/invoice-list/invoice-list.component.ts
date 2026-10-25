import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, debounceTime } from 'rxjs';
import { InvoicesApi } from '@core/api';
import { AuthService } from '@core/auth';
import { InvoiceQuery, InvoiceStatus, InvoiceSummary, Paged } from '@core/models';
import { LoadingComponent, PagerComponent, StatusBadgeComponent } from '@shared/ui';

@Component({
  selector: 'ac-invoice-list',
  imports: [CurrencyPipe, DatePipe, FormsModule, RouterLink, StatusBadgeComponent, PagerComponent, LoadingComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './invoice-list.component.html',
})
export class InvoiceListComponent implements OnInit {
  private readonly invoicesApi = inject(InvoicesApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly auth = inject(AuthService);

  readonly statuses: InvoiceStatus[] = ['Draft', 'Submitted', 'Approved', 'Rejected', 'Scheduled', 'Paid'];
  readonly query = signal<InvoiceQuery>({ page: 1, pageSize: 20, status: '', sort: '', search: '' });
  readonly result = signal<Paged<InvoiceSummary> | null>(null);
  readonly search$ = new Subject<string>();

  ngOnInit(): void {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(p => {
      this.query.set({
        page: Number(p.get('page') ?? 1),
        pageSize: 20,
        status: (p.get('status') as InvoiceStatus) ?? '',
        sort: p.get('sort') ?? '',
        search: p.get('search') ?? '',
        overdue: p.get('overdue') === 'true' ? true : null,
        vendorId: p.get('vendorId') ? Number(p.get('vendorId')) : null,
      });
      this.result.set(null);
      this.invoicesApi.list(this.query()).subscribe(r => this.result.set(r));
    });
    this.search$.pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef)).subscribe(search => this.apply({ search }));
  }

  /** Filters live in the URL so views are shareable and the back button works. */
  apply(change: Partial<InvoiceQuery>, resetPage = true): void {
    const next = { ...this.query(), ...change, ...(resetPage ? { page: 1 } : {}) };
    void this.router.navigate([], {
      queryParams: {
        status: next.status || null,
        sort: next.sort || null,
        search: next.search || null,
        overdue: next.overdue ? true : null,
        vendorId: next.vendorId ?? null,
        page: next.page && next.page > 1 ? next.page : null,
      },
    });
  }
}
