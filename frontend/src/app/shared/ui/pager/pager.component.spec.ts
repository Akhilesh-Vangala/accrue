import { TestBed } from '@angular/core/testing';
import { PagerComponent } from './pager.component';

describe('PagerComponent', () => {
  function render(page: number, total: number) {
    const fixture = TestBed.createComponent(PagerComponent);
    fixture.componentRef.setInput('page', page);
    fixture.componentRef.setInput('pageSize', 25);
    fixture.componentRef.setInput('total', total);
    fixture.detectChanges();
    return fixture;
  }

  it('shows the visible range and disables the ends', () => {
    const fixture = render(3, 60);
    const el = fixture.nativeElement as HTMLElement;
    const [prev, next] = Array.from(el.querySelectorAll('button'));

    expect(el.textContent).toContain('51-60 of 60');
    expect(prev.disabled).toBeFalse();
    expect(next.disabled).toBeTrue();
  });

  it('emits the next page', () => {
    const fixture = render(1, 60);
    const pages: number[] = [];
    fixture.componentInstance.pageChange.subscribe(p => pages.push(p));

    (fixture.nativeElement as HTMLElement).querySelectorAll('button')[1].click();

    expect(pages).toEqual([2]);
  });

  it('renders nothing for an empty list', () => {
    expect((render(1, 0).nativeElement as HTMLElement).textContent?.trim()).toBe('');
  });
});
