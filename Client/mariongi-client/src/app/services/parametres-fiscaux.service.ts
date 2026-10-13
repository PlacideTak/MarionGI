import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment.development';
import { AuthService } from '../login/auth.service';

import type {
  ParametreFiscalDto,
  CreerParametreFiscalRequest,
  ModifierParametreFiscalRequest
} from '../models/gestimmo.models';

@Injectable({
  providedIn: 'root'
})
export class ParametresFiscauxService {

  private readonly http = inject(HttpClient);

  private readonly authService = inject(AuthService);

  private readonly apiUrl =
    `${environment.apiUrl}/ParametresFiscaux`;

  // ============================================================
  // PERMISSIONS
  // ============================================================

  /**
   * Vérifie si l'utilisateur peut consulter les paramètres fiscaux.
   * À harmoniser avec les stratégies d'autorisation du backend.
   */
  canAccessParametresFiscaux(): boolean {

    return this.authService.hasRole([
      'Administrateur',
      'Admin',
      'Gestionnaire'
    ]);
  }

  /**
   * Seul le rôle Administrateur peut gérer les paramètres fiscaux.
   * Le backend reste la véritable source de sécurité.
   */
  canManageParametresFiscaux(): boolean {

    return this.authService.hasRole([
      'Administrateur'
    ]);
  }

  // ============================================================
  // LECTURE : LISTE ET FILTRES
  // ============================================================

  /**
   * Récupère les paramètres fiscaux non supprimés.
   *
   * Filtres facultatifs :
   * - anneeFiscale : année fiscale recherchée
   * - codeRegle    : code exact de la règle
   * - dateReference : date à laquelle le paramètre doit être valide
   */
  getParametresFiscaux(
    anneeFiscale?: number,
    codeRegle?: string,
    dateReference?: string | Date
  ): Observable<ParametreFiscalDto[]> {

    let params = new HttpParams();

    if (anneeFiscale !== undefined) {
      params = params.set(
        'anneeFiscale',
        anneeFiscale.toString()
      );
    }

    if (codeRegle?.trim()) {
      params = params.set(
        'codeRegle',
        codeRegle.trim()
      );
    }

    if (dateReference) {
      const date = dateReference instanceof Date
        ? dateReference.toISOString()
        : dateReference;

      params = params.set(
        'dateReference',
        date
      );
    }

    return this.http.get<ParametreFiscalDto[]>(
      this.apiUrl,
      { params }
    );
  }

  // ============================================================
  // LECTURE : DÉTAIL
  // ============================================================

  /**
   * Récupère un paramètre fiscal par son identifiant.
   */
  getParametreFiscalById(
    id: string
  ): Observable<ParametreFiscalDto> {

    return this.http.get<ParametreFiscalDto>(
      `${this.apiUrl}/${id}`
    );
  }

  // ============================================================
  // CRÉATION
  // ============================================================

  /**
   * Crée un nouveau paramètre fiscal.
   */
  createParametreFiscal(
    parametre: CreerParametreFiscalRequest
  ): Observable<ParametreFiscalDto> {

    return this.http.post<ParametreFiscalDto>(
      this.apiUrl,
      parametre
    );
  }

  // ============================================================
  // MODIFICATION
  // ============================================================

  /**
   * Modifie un paramètre fiscal existant.
   */
  updateParametreFiscal(
    id: string,
    parametre: ModifierParametreFiscalRequest
  ): Observable<ParametreFiscalDto> {

    return this.http.put<ParametreFiscalDto>(
      `${this.apiUrl}/${id}`,
      parametre
    );
  }

  // ============================================================
  // SUPPRESSION LOGIQUE
  // ============================================================

  /**
   * Supprime logiquement un paramètre fiscal.
   */
  deleteParametreFiscal(
    id: string
  ): Observable<void> {

    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }
}