// src/app/guards/auth.guard.ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../login/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Vérifie si un jeton/session valide existe
  if (authService.isAuthenticated()) {
    return true;
  }

  // Si non connecté, redirige vers la page de login en conservant l'URL tentée (optionnel)
  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url }
  });
};