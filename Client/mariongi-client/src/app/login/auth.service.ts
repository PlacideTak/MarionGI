import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, tap, throwError } from 'rxjs';

import { ApiError, AuthResponse, LoginPayload, VerifyOtpPayload } from './auth.models';
import { UserPayload, RoleUtilisateur, ROLES } from '../models/gestimmo.models';
import { environment } from '../../environments/environment.development';

const API_BASE = `${environment.apiUrl}/Auth`;
const TOKEN_KEY = 'accessToken';
const REFRESH_TOKEN_KEY = 'refreshToken';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  /** Signal réactif pour suivre l'utilisateur actuellement connecté */
  readonly currentUser = signal<UserPayload | null>(this.getUserFromToken());

  isAuthenticated(): boolean {
    const token = sessionStorage.getItem(TOKEN_KEY);
    return !!token && !!this.currentUser();
  }

  /**
   * POST /api/Auth/connexion
   */
  login(payload: LoginPayload): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${API_BASE}/connexion`, payload)
      .pipe(
        tap((res) => this.handleAuthSuccess(res)),
        catchError((err) => this.normalizeError(err))
      );
  }

  /**
   * POST /api/Auth/valider-otp
   */
  verifyOtp(payload: VerifyOtpPayload): Observable<AuthResponse> {
    const cleanPayload = {
      Telephone: payload.Telephone?.trim() || '',
      CodeOtp: payload.CodeOtp?.trim() || ''
    };

    return this.http
      .post<AuthResponse>(`${API_BASE}/valider-otp`, cleanPayload)
      .pipe(
        tap((res) => this.handleAuthSuccess(res)),
        catchError((err) => this.normalizeError(err))
      );
  }

  /**
   * POST /api/Auth/refresh-token
   */
  refreshToken(): Observable<AuthResponse> {
    const refreshToken = sessionStorage.getItem(REFRESH_TOKEN_KEY);
    return this.http
      .post<AuthResponse>(`${API_BASE}/refresh-token`, { refreshToken })
      .pipe(
        tap((res) => this.handleAuthSuccess(res)),
        catchError((err) => this.normalizeError(err))
      );
  }

  /**
   * Redéclenche l'envoi de l'OTP
   */
  resendOtp(payload: LoginPayload): Observable<AuthResponse> {
    return this.login(payload);
  }

  /**
   * Déconnexion complète.
   */
  logout(): void {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem('user_data');
    sessionStorage.clear();

    this.currentUser.set(null);
  }

  /**
   * Sauvegarde les tokens en cas de succès et met à jour le signal utilisateur.
   */
  private handleAuthSuccess(res: any): void {
    if (!res) return;

    // Gestion de la casse C# (PascalCase vs camelCase)
    const token = res.AccessToken ?? res.accessToken ?? res.token ?? res.Token;
    const refreshToken = res.RefreshToken ?? res.refreshToken;

    if (token) {
      sessionStorage.setItem(TOKEN_KEY, token);
      if (refreshToken) {
        sessionStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
      }
      this.currentUser.set(this.getUserFromToken());
    }
  }

  /**
   * Extrait et décode les infos utilisateur contenues dans le token JWT.
   */
  private getUserFromToken(): UserPayload | null {
    const token = sessionStorage.getItem(TOKEN_KEY);
    if (!token) return null;

    try {
      const payloadBase64 = token.split('.')[1];
      const decodedJson = atob(payloadBase64);
      const decoded = JSON.parse(decodedJson);

      return {
        id: decoded.sub || decoded.nameid,
        nom: decoded.family_name || decoded.Nom || '',
        prenom: decoded.given_name || decoded.Prenom || '',
        email: decoded.email || '',
        role: decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || 'Utilisateur',
      };
    } catch {
      return null;
    }
  }

  hasRole(allowedRoles: RoleUtilisateur | RoleUtilisateur[] | string | string[]): boolean {
    const user = this.currentUser();
    if (!user || !user.role) return false;

    const userRole = String(user.role).toLowerCase().trim();
    const roles = Array.isArray(allowedRoles) ? allowedRoles : [allowedRoles];

    const normalizedRoles = roles.flatMap(r => {
      const roleStr = String(r).toLowerCase().trim();
      
      // Si on passe l'énumération Administrateur ou le mot-clé administrateur/admin, 
      // on autorise les deux variantes via les constantes
      if (
        roleStr === ROLES.Administrateur || 
        roleStr === ROLES.Admin || 
        r === RoleUtilisateur.Administrateur
      ) {
        return [ROLES.Administrateur, ROLES.Admin];
      }
      
      return [roleStr];
    });

    return normalizedRoles.includes(userRole);
  }

  /**
   * Vérifie si l'utilisateur a la permission d'effectuer une action
   */
  hasPermission(permission: string): boolean {
    const token = sessionStorage.getItem(TOKEN_KEY);
    if (!token) return false;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const permissions = payload.permissions || payload['permission'] || [];
      return Array.isArray(permissions) && permissions.includes(permission);
    } catch {
      return false;
    }
  }

  /** Getters pratiques basés sur l'énumération centralisée */
  get isAdmin(): boolean {
    return this.hasRole(RoleUtilisateur.Administrateur);
  }

  get isGestionnaire(): boolean {
    return this.hasRole(RoleUtilisateur.Gestionnaire);
  }

  get isAgent(): boolean {
    return this.hasRole(RoleUtilisateur.Agent);
  }

  get isLocataire(): boolean {
    return this.hasRole(RoleUtilisateur.Locataire);
  }

  // Dans votre auth.service.ts

/**
 * Demande l'envoi d'un code ou d'un lien de récupération (par téléphone ou email)
 */
demanderRecuperation(identifiant: string): Observable<any> {
  return this.http.post(`${API_BASE}/mot-de-passe-oublie`, { identifiant }).pipe(
    catchError((err) => this.normalizeError(err))
  );
}

/**
 * Valide le code de récupération et définit un nouveau mot de passe
 */
reinitialiserMotDePasse(payload: any): Observable<any> {
  return this.http.post(`${API_BASE}/reinitialiser-mot-de-passe`, payload).pipe(
    catchError((err) => this.normalizeError(err))
  );
}

  private normalizeError(err: HttpErrorResponse) {
    const apiError: ApiError = {
      status: err.status,
      code: err.error?.code,
      message:
        err.error?.message ??
        (err.status === 429
          ? 'Trop de tentatives. Réessaie plus tard.'
          : err.status === 423
            ? 'Compte temporairement verrouillé. Réessaie plus tard.'
            : err.status === 403
              ? 'Ce compte est désactivé. Contacte un administrateur.'
              : err.status === 401
                ? 'Identifiant ou code OTP incorrect.'
                : err.status === 0
                  ? 'Impossible de contacter le serveur. Vérifie ta connexion ou le certificat HTTPS local.'
                  : 'Une erreur est survenue. Réessaie.'),
      retryAfterSeconds: err.error?.retryAfterSeconds,
    };
    return throwError(() => apiError);
  }
}