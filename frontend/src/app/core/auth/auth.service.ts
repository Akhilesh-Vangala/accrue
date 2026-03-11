import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, firstValueFrom, of, switchMap, tap } from 'rxjs';
import { Me, Role } from '../models';

/**
 * Cookie-based session against ASP.NET Core Identity. The SPA is served from the API's origin, so the
 * HttpOnly cookie also authorizes the Telerik report viewer and the Hangfire dashboard.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly current = signal<Me | null>(null);
  private loaded = false;

  readonly user = this.current.asReadonly();
  readonly isAuthenticated = computed(() => this.current() !== null);
  readonly isController = computed(() => this.hasRole('Controller'));
  readonly canEnterInvoices = computed(() => this.hasRole('Clerk', 'Controller'));
  readonly canApprove = computed(() => this.hasRole('Approver', 'Controller'));

  hasRole(...roles: Role[]): boolean {
    const user = this.current();
    return !!user && roles.some(r => user.roles.includes(r));
  }

  /** Resolves the session once per page load; guards await this. */
  async ensureLoaded(): Promise<Me | null> {
    if (!this.loaded) {
      await firstValueFrom(this.refresh());
    }
    return this.current();
  }

  refresh(): Observable<Me | null> {
    return this.http.get<Me>('/api/auth/me').pipe(
      catchError(() => of(null)),
      tap(me => {
        this.current.set(me);
        this.loaded = true;
      }),
    );
  }

  login(email: string, password: string): Observable<Me | null> {
    return this.http
      .post('/api/auth/login', { email, password }, { params: { useCookies: true } })
      .pipe(switchMap(() => this.refresh()));
  }

  logout(): void {
    this.http.post('/api/auth/logout', {}).pipe(catchError(() => of(null))).subscribe(() => {
      this.clear();
      void this.router.navigate(['/login']);
    });
  }

  clear(): void {
    this.current.set(null);
    this.loaded = true;
  }

  displayInitials = computed(() =>
    (this.current()?.displayName ?? '')
      .split(' ')
      .map(part => part[0])
      .join('')
      .slice(0, 2)
      .toUpperCase(),
  );

  roleLabel = computed(() => this.current()?.roles.join(', ') ?? '');
}
