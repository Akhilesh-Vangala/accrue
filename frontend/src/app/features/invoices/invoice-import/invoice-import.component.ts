import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { InvoicesApi } from '@core/api';
import { describeError } from '@core/http';
import { ExtractionCapabilities, InvoiceExtraction } from '@core/models';
import { blockingFindings } from './extraction-mapping';

/** Upload a vendor invoice and hand the extracted draft to the form. The user still reviews and saves. */
@Component({
  selector: 'lf-invoice-import',
  imports: [DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './invoice-import.component.html',
  styleUrl: './invoice-import.component.scss',
})
export class InvoiceImportComponent {
  readonly capabilities = input.required<ExtractionCapabilities>();
  readonly extracted = output<{ extraction: InvoiceExtraction; fileName: string }>();

  private readonly invoicesApi = inject(InvoicesApi);

  readonly busy = signal(false);
  readonly dragging = signal(false);
  readonly error = signal<string | null>(null);
  readonly result = signal<InvoiceExtraction | null>(null);
  readonly fileName = signal<string | null>(null);

  readonly blocking = () => blockingFindings(this.result()?.findings ?? []);

  onFileInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (file) {
      this.read(file);
    }
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    const file = event.dataTransfer?.files?.[0];
    if (file) {
      this.read(file);
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(true);
  }

  private read(file: File): void {
    const caps = this.capabilities();
    if (!caps.acceptedTypes.includes(file.type)) {
      this.error.set('Upload a PDF, PNG or JPEG invoice.');
      return;
    }
    if (file.size > caps.maxFileBytes) {
      this.error.set(`Invoices must be ${Math.round(caps.maxFileBytes / 1_048_576)} MB or smaller.`);
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    this.result.set(null);
    this.fileName.set(file.name);
    this.invoicesApi.extract(file).subscribe({
      next: extraction => {
        this.busy.set(false);
        this.result.set(extraction);
        this.extracted.emit({ extraction, fileName: file.name });
      },
      error: err => {
        this.busy.set(false);
        this.error.set(describeError(err));
      },
    });
  }
}
