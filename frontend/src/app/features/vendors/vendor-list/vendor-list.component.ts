import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { VendorsApi } from '@core/api';
import { AuthService } from '@core/auth';
import { describeError, fieldErrors } from '@core/http';
import { Paged, Vendor } from '@core/models';
import { ToastService } from '@core/toast';
import { LoadingComponent, PagerComponent } from '@shared/ui';

@Component({
  selector: 'lf-vendor-list',
  imports: [CurrencyPipe, FormsModule, ReactiveFormsModule, RouterLink, LoadingComponent, PagerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './vendor-list.component.html',
})
export class VendorListComponent {
  private readonly vendorsApi = inject(VendorsApi);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);

  readonly result = signal<Paged<Vendor> | null>(null);
  readonly categories = signal<string[]>([]);
  readonly editing = signal<Vendor | 'new' | null>(null);
  readonly saving = signal(false);
  readonly errors = signal<Partial<Record<string, string[]>>>({});
  search = '';
  includeInactive = false;
  page = 1;

  readonly form = inject(FormBuilder).nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    taxId: ['', [Validators.required, Validators.pattern(/^\d{2}-\d{7}$/)]],
    email: ['', [Validators.required, Validators.email]],
    category: ['', Validators.required],
    paymentTermsDays: [30, [Validators.required, Validators.min(0), Validators.max(120)]],
  });

  constructor() {
    this.vendorsApi.categories().subscribe(c => this.categories.set(c));
    this.load();
  }

  load(page = 1): void {
    this.page = page;
    this.vendorsApi.list({ search: this.search, includeInactive: this.includeInactive, page, pageSize: 20 }).subscribe(r => this.result.set(r));
  }

  edit(vendor: Vendor | 'new'): void {
    this.errors.set({});
    this.editing.set(vendor);
    if (vendor === 'new') {
      this.form.reset({ name: '', taxId: '', email: '', category: '', paymentTermsDays: 30 });
    } else {
      this.form.reset({ name: vendor.name, taxId: vendor.taxId, email: vendor.email, category: vendor.category, paymentTermsDays: vendor.paymentTermsDays });
    }
  }

  save(): void {
    this.form.markAllAsTouched();
    const target = this.editing();
    if (this.form.invalid || !target) {
      return;
    }
    this.saving.set(true);
    const body = { ...this.form.getRawValue(), rowVersion: target === 'new' ? null : target.rowVersion };
    const request = target === 'new' ? this.vendorsApi.create(body) : this.vendorsApi.update(target.id, body);
    request.subscribe({
      next: v => {
        this.saving.set(false);
        this.editing.set(null);
        this.toast.success(`${v.name} saved.`);
        this.load(this.page);
      },
      error: err => {
        this.saving.set(false);
        this.errors.set(fieldErrors(err));
        this.toast.error(describeError(err));
      },
    });
  }

  toggleActive(vendor: Vendor): void {
    this.vendorsApi.setActive(vendor.id, !vendor.isActive).subscribe({
      next: v => {
        this.toast.success(`${v.name} ${v.isActive ? 'reactivated' : 'deactivated'}.`);
        this.load(this.page);
      },
      error: err => this.toast.error(describeError(err)),
    });
  }

  invalid(name: keyof typeof this.form.controls): boolean {
    const c = this.form.controls[name];
    return (c.invalid && c.touched) || !!this.errors()[name];
  }
}
