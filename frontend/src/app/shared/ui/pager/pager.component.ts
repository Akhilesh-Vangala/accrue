import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

/** Previous/next pager for server-side paged lists. */
@Component({
  selector: 'ac-pager',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './pager.component.html',
})
export class PagerComponent {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly total = input.required<number>();
  readonly pageChange = output<number>();
  readonly pages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));
  readonly from = computed(() => (this.page() - 1) * this.pageSize() + 1);
  readonly to = computed(() => Math.min(this.total(), this.page() * this.pageSize()));
}
