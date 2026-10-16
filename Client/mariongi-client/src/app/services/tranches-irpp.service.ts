import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment.development';
import { AuthService } from '../login/auth.service';

import type {
  CreerTrancheIRPPRequest,
  ModifierTrancheIRPPRequest,
  TrancheIRPPDto
} from '../models/gestimmo.models';

@Injectable({
  providedIn: 'root'
})
export class TranchesIRPPService {

  private readonly http = inject(HttpClient);

  private readonly authService = inject(AuthService);

  private readonly apiUrl =
    `${environment.apiUrl}/TranchesIRPP`;


  // ============================================================
  // PERMISSIONS
  // ============================================================

  /**
   * Vérifie si l'utilisateur peut consulter les tranches IRPP.
   */
  canAccessTranchesIRPP(): boolean {
    return this.authService.hasRole([
      'Administrateur',
      'Admin',
      'Gestionnaire'
    ]);
  }

  /**
   * Seul le rôle Administrateur peut gérer les tranches IRPP.
   * Le backend reste la véritable source de sécurité.
   */
  canManageTranchesIRPP(): boolean {
    return this.authService.hasRole([
      'Administrateur'
    ]);
  }


  // ============================================================
  // LECTURE
  // ============================================================

  /**
   * Récupère les tranches IRPP.
   *
   * anneeFiscale non définie : toutes les années fiscales.
   * anneeFiscale définie      : tranches de l'année sélectionnée.
   */
  getTranchesIRPP(
    anneeFiscale?: number
  ): Observable<TrancheIRPPDto[]> {

    let params = new HttpParams();

    if (anneeFiscale !== undefined) {
      params = params.set(
        'anneeFiscale',
        anneeFiscale.toString()
      );
    }

    return this.http.get<TrancheIRPPDto[]>(
      this.apiUrl,
      { params }
    );
  }


  /**
   * Récupère une tranche IRPP par son identifiant.
   */
  getTrancheIRPPById(
    id: string
  ): Observable<TrancheIRPPDto> {

    return this.http.get<TrancheIRPPDto>(
      `${this.apiUrl}/${id}`
    );
  }


  // ============================================================
  // CRÉATION
  // ============================================================

  /**
   * Crée une nouvelle tranche IRPP.
   */
  createTrancheIRPP(
    tranche: CreerTrancheIRPPRequest
  ): Observable<TrancheIRPPDto> {

    return this.http.post<TrancheIRPPDto>(
      this.apiUrl,
      tranche
    );
  }


  // ============================================================
  // MODIFICATION
  // ============================================================

  /**
   * Modifie une tranche IRPP existante.
   */
  updateTrancheIRPP(
    id: string,
    tranche: ModifierTrancheIRPPRequest
  ): Observable<TrancheIRPPDto> {

    return this.http.put<TrancheIRPPDto>(
      `${this.apiUrl}/${id}`,
      tranche
    );
  }


  // ============================================================
  // SUPPRESSION
  // ============================================================

  /**
   * Suppression logique d'une tranche IRPP.
   */
  deleteTrancheIRPP(
    id: string
  ): Observable<{ message: string }> {

    return this.http.delete<{ message: string }>(
      `${this.apiUrl}/${id}`
    );
  }

}