import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { interval, startWith, switchMap } from 'rxjs';
import { NotificationsApi } from '@core/api';
import { AuthService } from '@core/auth';
import { NotificationFeed, NotificationItem } from '@core/models';
import { ToastService } from '@core/toast';

interface NavItem {
  path: string;
  label: string;
  icon: string;
  visible: () => boolean;
}

@Component({
  selector: 'lf-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly toasts = inject(ToastService);
  private readonly notificationsApi = inject(NotificationsApi);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly feed = signal<NotificationFeed>({ unreadCount: 0, items: [] });
  readonly bellOpen = signal(false);
  readonly navOpen = signal(false);

  readonly nav: NavItem[] = [
    { path: '/dashboard', label: 'Dashboard', icon: 'bi-speedometer2', visible: () => true },
    { path: '/invoices', label: 'Invoices', icon: 'bi-receipt', visible: () => true },
    { path: '/approvals', label: 'Approvals', icon: 'bi-check2-square', visible: () => this.auth.canApprove() },
    { path: '/payment-runs', label: 'Payment runs', icon: 'bi-bank', visible: () => true },
    { path: '/vendors', label: 'Vendors', icon: 'bi-buildings', visible: () => true },
    { path: '/reports', label: 'Reports', icon: 'bi-file-earmark-bar-graph', visible: () => true },
    { path: '/jobs', label: 'Background jobs', icon: 'bi-clock-history', visible: () => this.auth.isController() },
  ];

  ngOnInit(): void {
    interval(30_000)
      .pipe(
        startWith(0),
        switchMap(() => this.notificationsApi.feed()),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({ next: feed => this.feed.set(feed), error: () => undefined });
  }

  open(item: NotificationItem): void {
    this.bellOpen.set(false);
    if (!item.isRead) {
      this.notificationsApi.markRead(item.id).subscribe(() => this.reloadFeed());
    }
    if (item.link) {
      void this.router.navigateByUrl(item.link);
    }
  }

  markAllRead(): void {
    this.notificationsApi.markAllRead().subscribe(() => this.reloadFeed());
  }

  private reloadFeed(): void {
    this.notificationsApi.feed().subscribe(feed => this.feed.set(feed));
  }
}
