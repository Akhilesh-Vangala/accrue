import { TestBed } from '@angular/core/testing';
import { StatusBadgeComponent } from './status-badge.component';

describe('StatusBadgeComponent', () => {
  it('colors the status and adds an overdue badge', () => {
    const fixture = TestBed.createComponent(StatusBadgeComponent);
    fixture.componentRef.setInput('status', 'Paid');
    fixture.detectChanges();

    const badges = (fixture.nativeElement as HTMLElement).querySelectorAll('.badge');
    expect(badges.length).toBe(1);
    expect(badges[0].classList).toContain('text-bg-success');

    fixture.componentRef.setInput('status', 'Submitted');
    fixture.componentRef.setInput('overdue', true);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Overdue');
  });
});
