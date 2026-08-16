import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * Same silent-SSO-refresh flow as {@link authGuard}, plus an `Admin` role check. A signed-in
 * non-admin is bounced to the home page rather than to the IdP login (they ARE signed in, they
 * just can't manage master data) — an unauthenticated visitor still gets sent to sign in first.
 */
export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const afterAuthed = () => (auth.hasRole('Admin') ? true : router.createUrlTree(['/']));

  if (auth.isAuthenticated()) return of(afterAuthed());

  return auth.refresh().pipe(
    map(() => afterAuthed()),
    catchError(() => {
      auth.forceClear();
      auth.loginRedirect();
      return of(false);
    })
  );
};
