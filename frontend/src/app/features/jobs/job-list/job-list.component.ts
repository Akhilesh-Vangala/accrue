import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { JobsApi } from '@core/api';
import { describeError } from '@core/http';
import { RecurringJob } from '@core/models';
import { ToastService } from '@core/toast';
import { LoadingComponent } from '@shared/ui';

@Component({
  selector: 'ac-job-list',
  imports: [DatePipe, LoadingComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './job-list.component.html',
})
export class JobListComponent {
  private readonly jobsApi = inject(JobsApi);
  private readonly toast = inject(ToastService);
  readonly jobs = signal<RecurringJob[] | null>(null);
  readonly busy = signal<string | null>(null);

  constructor() {
    this.load();
  }

  trigger(job: RecurringJob): void {
    this.busy.set(job.id);
    this.jobsApi.trigger(job.id).subscribe({
      next: r => {
        this.toast.success(`Queued ${job.id} as background job #${r.backgroundJobId}.`);
        setTimeout(() => {
          this.busy.set(null);
          this.load();
        }, 2500);
      },
      error: err => {
        this.busy.set(null);
        this.toast.error(describeError(err));
      },
    });
  }

  private load(): void {
    this.jobsApi.list().subscribe(j => this.jobs.set(j));
  }
}
