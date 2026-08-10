import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

// ✅ Usage in route data: data: { roles: ['Admin', 'Staff'] }
export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const roles = (route.data?.['roles'] as string[]) || [];

  if (!auth.isAuthenticated()) {
    router.navigate(['/login']);
    return false;
  }
  if (roles.length === 0 || auth.hasRole(...roles)) return true;

  // optional: redirect to a 403 page; for now, go dashboard
  router.navigate(['/dashboard']);
  return false;
};
