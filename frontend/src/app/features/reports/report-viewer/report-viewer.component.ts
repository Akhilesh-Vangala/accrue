import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TelerikReportingModule } from '@progress/telerik-angular-report-viewer';

export interface ViewerSource {
  report: string;
  parameters: Record<string, string | number | null>;
}

/** Wraps the Telerik viewer so the reports page can @defer it; it pulls in jQuery and ~2 MB of viewer code. */
@Component({
  selector: 'ac-report-viewer',
  imports: [TelerikReportingModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './report-viewer.component.html',
})
export class ReportViewerComponent {
  readonly source = input.required<ViewerSource>();
}
