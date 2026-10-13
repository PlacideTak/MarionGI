import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment.development';
import { AuthService } from '../login/auth.service';

import type {
  CreerRegimeFiscalRequest,
  ModifierRegimeFiscalRequest,
  RegimeFiscalDto
} from '../models/gestimmo.models';


@Injectable({
  providedIn: 'root'
})
export class RegimesFiscauxService {

  private readonly http = inject(HttpClient);

  private readonly authService = inject(AuthService);

  private readonly apiUrl =
    `${environment.apiUrl}/RegimesFiscaux`;


  // ============================================================
  // PERMISSIONS
  // ============================================================

  /**
   * Vérifie si l'utilisateur peut consulter les régimes fiscaux.
   */
  canAccessRegimesFiscaux(): boolean {

    return this.authService.hasRole([
      'Administrateur',
      'Admin',
      'Gestionnaire'
    ]);
  }


  /**
   * Seul le rôle Administrateur peut gérer les régimes fiscaux.
   * Le backend reste la véritable source de sécurité.
   */
  canManageRegimesFiscaux(): boolean {

    return this.authService.hasRole([
      'Administrateur'
    ]);
  }


  // ============================================================
  // LECTURE
  // ============================================================

  /**
   * Récupère les régimes fiscaux non supprimés.
   *
   * estActif non défini : tous les régimes.
   * estActif = true      : régimes actifs.
   * estActif = false     : régimes inactifs.
   */
  getRegimesFiscaux(
    estActif?: boolean
  ): Observable<RegimeFiscalDto[]> {

    const url = estActif === undefined
      ? this.apiUrl
      : `${this.apiUrl}?estActif=${estActif}`;

    return this.http.get<RegimeFiscalDto[]>(url);
  }


  /**
   * Récupère un régime fiscal par son identifiant.
   */
  getRegimeFiscalById(
    id: string
  ): Observable<RegimeFiscalDto> {

    return this.http.get<RegimeFiscalDto>(
      `${this.apiUrl}/${id}`
    );
  }


  // ============================================================
  // CRÉATION
  // ============================================================

  /**
   * Crée un nouveau régime fiscal.
   */
  createRegimeFiscal(
    regime: CreerRegimeFiscalRequest
  ): Observable<RegimeFiscalDto> {

    return this.http.post<RegimeFiscalDto>(
      this.apiUrl,
      regime
    );
  }


  // ============================================================
  // MODIFICATION
  // ============================================================

  /**
   * Modifie un régime fiscal existant.
   */
  updateRegimeFiscal(
    id: string,
    regime: ModifierRegimeFiscalRequest
  ): Observable<RegimeFiscalDto> {

    return this.http.put<RegimeFiscalDto>(
      `${this.apiUrl}/${id}`,
      regime
    );
  }


  // ============================================================
  // SUPPRESSION
  // ============================================================

  /**
   * Suppression logique du régime fiscal.
   */
  deleteRegimeFiscal(
    id: string
  ): Observable<{ message: string }> {

    return this.http.delete<{ message: string }>(
      `${this.apiUrl}/${id}`
    );
  }

}