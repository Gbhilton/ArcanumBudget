import { inject } from '@angular/core';
import { CanActivateFn, Router, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (_route, state: RouterStateSnapshot) => {
  const auth = inject(AuthService);
  if (auth.isAuthenticated()) return true;

  inject(Router).navigate(['/login'], { queryParams: { returnUrl: state.url } });
  return false;
};
