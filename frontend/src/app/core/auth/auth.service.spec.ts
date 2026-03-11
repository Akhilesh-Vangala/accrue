import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';
import { Me } from '../models';

describe('AuthService', () => {
  let auth: AuthService;
  let backend: HttpTestingController;

  const approver: Me = { email: 'approver@ledgerflow.dev', displayName: 'Marcus Lee', roles: ['Approver'] };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    auth = TestBed.inject(AuthService);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('signs in with a cookie session and loads the profile', () => {
    let signedIn: Me | null = null;
    auth.login('approver@ledgerflow.dev', 'secret').subscribe(me => (signedIn = me));

    const login = backend.expectOne(r => r.url === '/api/auth/login');
    expect(login.request.params.get('useCookies')).toBe('true');
    expect(login.request.body).toEqual({ email: 'approver@ledgerflow.dev', password: 'secret' });
    login.flush({});
    backend.expectOne('/api/auth/me').flush(approver);

    expect(signedIn!).toEqual(approver);
    expect(auth.isAuthenticated()).toBeTrue();
    expect(auth.displayInitials()).toBe('ML');
  });

  it('derives permissions from roles', () => {
    auth.refresh().subscribe();
    backend.expectOne('/api/auth/me').flush(approver);

    expect(auth.canApprove()).toBeTrue();
    expect(auth.canEnterInvoices()).toBeFalse();
    expect(auth.isController()).toBeFalse();
    expect(auth.hasRole('Clerk', 'Approver')).toBeTrue();
  });

  it('treats a failed session probe as signed out and only probes once', async () => {
    const first = auth.ensureLoaded();
    backend.expectOne('/api/auth/me').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(await first).toBeNull();
    expect(await auth.ensureLoaded()).toBeNull();
    backend.expectNone('/api/auth/me');
  });
});
