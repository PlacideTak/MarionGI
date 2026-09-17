import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  
  // 1. Récupération du token (vérifie sessionStorage, puis localStorage)
  const token = sessionStorage.getItem('accessToken') || localStorage.getItem('accessToken') || localStorage.getItem('token');

  // 2. Injection du header Authorization si le token existe
  let authReq = req;
  if (token) {
    authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // 3. Traitement des erreurs 401 (exclut l'appel API de login pour éviter les boucles)
      if (error.status === 401 && !req.url.includes('/login') && !req.url.includes('/auth')) {
        sessionStorage.clear();
        localStorage.clear();
        router.navigate(['/login']);
      }
      return throwError(() => error);
    })
  );
};