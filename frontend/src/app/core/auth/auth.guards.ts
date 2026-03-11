import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Role } from '../models';
import { AuthService } from './auth.service';

/** Requires a signed-in user; otherwise redirects to login and comes back afterwards. */
export const authGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return (await auth.ensureLoaded()) ? true : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/** Requires one of the given roles; otherwise sends the user to the dashboard. The API enforces the same rules. */
export function roleGuard(...roles: Role[]): CanActivateFn {
  return async () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    await auth.ensureLoaded();
    return auth.hasRole(...roles) ? true : router.createUrlTree(['/dashboard']);
  };
}
