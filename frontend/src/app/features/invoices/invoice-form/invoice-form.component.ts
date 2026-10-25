import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { InvoicesApi, VendorsApi } from '@core/api';
import { describeError, fieldErrors } from '@core/http';
import { ExtractionCapabilities, InvoiceExtraction, InvoiceLine, SaveInvoice, Vendor } from '@core/models';
import { ToastService } from '@core/toast';
import { LoadingComponent } from '@shared/ui';
import { isoDate } from '@shared/utils';
import { flaggedFields } from '../invoice-import/extraction-mapping';
import { InvoiceImportComponent } from '../invoice-import/invoice-import.component';

type LineForm = FormGroup<{
  description: FormControl<string>;
  glAccount: FormControl<string>;
  quantity: FormControl<number>;
  unitPrice: FormControl<number>;
}>;

@Component({
  selector: 'lf-invoice-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe, LoadingComponent, InvoiceImportComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './invoice-form.component.html',
})
export class InvoiceFormComponent implements OnInit {
  /** Bound from the route (:id) via withComponentInputBinding. */
  readonly id = input<string>();

  private readonly invoicesApi = inject(InvoicesApi);
  private readonly vendorsApi = inject(VendorsApi);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder).nonNullable;

  readonly vendors = signal<Vendor[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly errors = signal<Partial<Record<string, string[]>>>({});
  readonly extraction = signal<ExtractionCapabilities | null>(null);
  readonly flagged = signal<Set<string>>(new Set());
  private rowVersion: string | null = null;

  readonly form = this.fb.group({
    vendorId: [0, [Validators.required, Validators.min(1)]],
    invoiceNumber: ['', [Validators.required, Validators.maxLength(50)]],
    invoiceDate: [isoDate(new Date()), Validators.required],
    taxAmount: [0, [Validators.required, Validators.min(0)]],
    notes: [''],
    lines: this.fb.array<LineForm>([]),
  });

  private readonly formValue = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  readonly subtotal = computed(() =>
    (this.formValue().lines ?? []).reduce((sum, l) => sum + Math.round((l?.quantity ?? 0) * (l?.unitPrice ?? 0) * 100) / 100, 0),
  );
  readonly total = computed(() => this.subtotal() + Number(this.formValue().taxAmount ?? 0));
  readonly needsController = computed(() => this.total() > 10_000);
  readonly selectedVendor = computed(() => this.vendors().find(v => v.id === Number(this.formValue().vendorId)));
  readonly isEdit = computed(() => !!this.id());

  get lines(): FormArray<LineForm> {
    return this.form.controls.lines;
  }

  ngOnInit(): void {
    this.vendorsApi.list({ pageSize: 100 }).subscribe(r => this.vendors.set(r.items));
    const id = this.id();
    if (!id) {
      this.invoicesApi.extractionCapabilities().subscribe({ next: caps => this.extraction.set(caps.enabled ? caps : null), error: () => undefined });
      this.addLine();
      this.loading.set(false);
      return;
    }

    this.invoicesApi.get(Number(id)).subscribe(detail => {
      this.rowVersion = detail.rowVersion;
      detail.lines.forEach(l => this.addLine(l));
      this.form.patchValue({
        vendorId: detail.summary.vendorId,
        invoiceNumber: detail.summary.invoiceNumber,
        invoiceDate: detail.summary.invoiceDate,
        taxAmount: detail.taxAmount,
        notes: detail.notes ?? '',
      });
      this.form.controls.vendorId.disable();
      this.loading.set(false);
    });
  }

  addLine(line?: InvoiceLine): void {
    this.lines.push(
      this.fb.group({
        description: [line?.description ?? '', [Validators.required, Validators.maxLength(300)]],
        glAccount: [line?.glAccount ?? this.defaultGl(), [Validators.required, Validators.pattern(/^\d{4}$/)]],
        quantity: [line?.quantity ?? 1, [Validators.required, Validators.min(0.01)]],
        unitPrice: [line?.unitPrice ?? 0, [Validators.required, Validators.min(0)]],
      }),
    );
  }

  /** Replaces the form with an extracted draft. Nothing is saved; flagged fields are highlighted for review. */
  applyExtraction({ extraction, fileName }: { extraction: InvoiceExtraction; fileName: string }): void {
    const draft = extraction.draft;
    this.lines.clear();
    (draft.lines.length > 0 ? draft.lines : [undefined]).forEach(l => this.addLine(l));
    this.form.patchValue({
      vendorId: draft.vendorId ?? 0,
      invoiceNumber: draft.invoiceNumber ?? '',
      invoiceDate: draft.invoiceDate ?? isoDate(new Date()),
      taxAmount: draft.taxAmount,
      notes: `Imported from ${fileName}.`,
    });
    this.flagged.set(flaggedFields(extraction));
    this.form.markAllAsTouched();
  }

  isFlagged(path: string): boolean {
    return this.flagged().has(path);
  }

  removeLine(index: number): void {
    this.lines.removeAt(index);
  }

  lineTotal(index: number): number {
    const l = this.lines.at(index).getRawValue();
    return Math.round(l.quantity * l.unitPrice * 100) / 100;
  }

  save(submitAfter: boolean): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.error.set('Fix the highlighted fields first.');
      return;
    }

    const raw = this.form.getRawValue();
    const body: SaveInvoice = {
      vendorId: Number(raw.vendorId),
      invoiceNumber: raw.invoiceNumber.trim(),
      invoiceDate: raw.invoiceDate,
      taxAmount: Number(raw.taxAmount),
      notes: raw.notes.trim() || null,
      lines: raw.lines.map(l => ({ ...l, quantity: Number(l.quantity), unitPrice: Number(l.unitPrice) })),
      rowVersion: this.rowVersion,
    };

    this.saving.set(true);
    this.error.set(null);
    this.errors.set({});
    const id = this.id();
    const request = id ? this.invoicesApi.update(Number(id), body) : this.invoicesApi.create(body);
    request.subscribe({
      next: saved => {
        if (!submitAfter) {
          this.toast.success(`Invoice ${saved.summary.invoiceNumber} saved as draft.`);
          void this.router.navigate(['/invoices', saved.summary.id]);
          return;
        }
        this.invoicesApi.act(saved.summary.id, 'submit').subscribe({
          next: () => {
            this.toast.success(`Invoice ${saved.summary.invoiceNumber} submitted for approval.`);
            void this.router.navigate(['/invoices', saved.summary.id]);
          },
          error: err => {
            this.toast.error(describeError(err));
            void this.router.navigate(['/invoices', saved.summary.id]);
          },
        });
      },
      error: err => {
        this.saving.set(false);
        this.error.set(describeError(err));
        this.errors.set(fieldErrors(err));
      },
    });
  }

  invalid(path: string): boolean {
    const control = this.form.get(path);
    return !!control && control.invalid && control.touched;
  }

  private defaultGl(): string {
    const category = this.selectedVendor()?.category;
    const map: Record<string, string> = {
      Cloud: '6110', Software: '6100', Facilities: '6400', 'Professional Services': '6300',
      Logistics: '6500', Hardware: '1510', Marketing: '6200', Utilities: '6600',
    };
    return (category && map[category]) || '6000';
  }
}
