
import { Injectable, inject } from '@angular/core';
import {
  HttpClient,
  HttpParams
} from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment.development';
import { AuthService } from '../login/auth.service';

import type {
  PrestataireDto,
  CreerPrestataireRequest,
  ModifierPrestataireRequest,
  ModifierStatutPrestataireRequest
} from '../models/gestimmo.models';

@Injectable({
  providedIn: 'root'
})
export class PrestatairesService {

  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);

  private readonly apiUrl =
    `${environment.apiUrl}/Prestataires`;

  // ============================================================
  // PERMISSIONS
  // ============================================================

  canAccessPrestataires(): boolean {
    return this.authService.hasRole([
      'Administrateur',
      'Admin',
      'Gestionnaire',
      'Agent'
    ]);
  }

  canManagePrestataires(): boolean {
    return this.authService.hasRole([
      'Administrateur',
      'Admin',
      'Gestionnaire'
    ]);
  }

  // ============================================================
  // LECTURE
  // ============================================================

  /**
   * Récupère les prestataires avec filtres facultatifs.
   */
  getPrestataires(
    recherche?: string,
    typePrestataire?: number,
    actif?: boolean
  ): Observable<PrestataireDto[]> {

    let params = new HttpParams();

    if (recherche?.trim()) {
      params = params.set('recherche', recherche.trim());
    }

    if (typePrestataire !== undefined) {
      params = params.set(
        'typePrestataire',
        typePrestataire.toString()
      );
    }

    if (actif !== undefined) {
      params = params.set('actif', actif.toString());
    }

    return this.http.get<PrestataireDto[]>(
      this.apiUrl,
      { params }
    );
  }

  /**
   * Récupère un prestataire par son identifiant.
   */
  getPrestataireById(
    id: string
  ): Observable<PrestataireDto> {

    return this.http.get<PrestataireDto>(
      `${this.apiUrl}/${id}`
    );
  }

  // ============================================================
  // CRÉATION
  // ============================================================

  createPrestataire(
    prestataire: CreerPrestataireRequest
  ): Observable<{ message: string; id: string }> {

    return this.http.post<{ message: string; id: string }>(
      this.apiUrl,
      prestataire
    );
  }

  // ============================================================
  // MODIFICATION
  // ============================================================

  updatePrestataire(
    id: string,
    prestataire: ModifierPrestataireRequest
  ): Observable<{ message: string }> {

    return this.http.put<{ message: string }>(
      `${this.apiUrl}/${id}`,
      prestataire
    );
  }

  // ============================================================
  // ACTIVATION / DÉSACTIVATION
  // ============================================================

  modifierStatut(
    id: string,
    actif: boolean
  ): Observable<{ message: string; actif: boolean }> {

    const body: ModifierStatutPrestataireRequest = { actif };

    return this.http.patch<{ message: string; actif: boolean }>(
      `${this.apiUrl}/${id}/statut`,
      body
    );
  }

  // ============================================================
  // SUPPRESSION
  // ============================================================

  deletePrestataire(
    id: string
  ): Observable<{ message: string }> {

    return this.http.delete<{ message: string }>(
      `${this.apiUrl}/${id}`
    );
  }
}
