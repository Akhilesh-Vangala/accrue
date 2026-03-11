import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let router: jasmine.SpyObj<Router>;
  let auth: jasmine.SpyObj<AuthService>;

  beforeEach(() => {
    router = jasmine.createSpyObj<Router>('Router', ['navigate'], { url: '/invoices?status=Submitted' });
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['clear']);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
        { provide: AuthService, useValue: auth },
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('sends an expired session back to login with a return url', () => {
    http.get('/api/invoices').subscribe({ error: () => undefined });
    backend.expectOne('/api/invoices').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(auth.clear).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { returnUrl: '/invoices?status=Submitted' } });
  });

  it('leaves the session probe alone', () => {
    http.get('/api/auth/me').subscribe({ error: () => undefined });
    backend.expectOne('/api/auth/me').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('does not log out on forbidden', () => {
    http.post('/api/payment-runs/1/release', {}).subscribe({ error: () => undefined });
    backend.expectOne('/api/payment-runs/1/release').flush(null, { status: 403, statusText: 'Forbidden' });

    expect(auth.clear).not.toHaveBeenCalled();
  });
});
