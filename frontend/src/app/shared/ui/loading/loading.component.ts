import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'ac-loading',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="d-flex align-items-center justify-content-center gap-2 py-5 text-muted-2">
      <span class="spinner-border spinner-border-sm"></span> Loading...
    </div>
  `,
})
export class LoadingComponent {}
