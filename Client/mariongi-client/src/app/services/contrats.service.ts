import { Injectable, inject } from '@angular/core';
import {
  HttpClient,
  HttpParams
} from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  ContratDto,
  CreerContratRequest,
  ModifierContratRequest,
  StatutContrat,
  ROLES
} from '../models/gestimmo.models';

import { environment } from '../../environments/environment.development';
import { AuthService } from '../login/auth.service';


@Injectable({
  providedIn: 'root'
})
export class ContratsService {

  private readonly http =
    inject(HttpClient);

  private readonly authService =
    inject(AuthService);

  private readonly apiUrl =
    `${environment.apiUrl}/Contrats`;


  // ============================================================
  // PERMISSIONS
  // ============================================================

  /**
   * Rôles autorisés à gérer les contrats.
   *
   * Le backend reste la véritable source de sécurité.
   * Cette méthode sert principalement à contrôler l'interface.
   */
  canManageContrats(): boolean {

    return this.authService.hasRole([
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire
    ]);
  }


  /**
   * Rôles pouvant accéder à la fonctionnalité Contrats.
   *
   * Le locataire peut consulter ses propres contrats.
   *
   * L'Agent n'est volontairement pas inclus ici,
   * car le backend ne lui retourne actuellement aucun contrat.
   */
  canAccessContrats(): boolean {

    return this.authService.hasRole([
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire,
      ROLES.Locataire
    ]);
  }


  // ============================================================
  // LECTURE
  // ============================================================

  /**
   * Récupère les contrats.
   *
   * Pour un locataire, l'API doit automatiquement
   * limiter le résultat à ses propres contrats.
   */
  getContrats(
    statut?: StatutContrat
  ): Observable<ContratDto[]> {

    let params = new HttpParams();

    if (
      statut !== undefined &&
      statut !== null
    ) {

      params =
        params.set(
          'statut',
          statut.toString()
        );
    }

    return this.http.get<ContratDto[]>(
      this.apiUrl,
      { params }
    );
  }


  /**
   * Récupère un contrat par son identifiant.
   *
   * Le backend doit également vérifier que
   * le locataire connecté possède ce contrat.
   */
  getContratById(
    id: string
  ): Observable<ContratDto> {

    return this.http.get<ContratDto>(
      `${this.apiUrl}/${id}`
    );
  }


  // ============================================================
  // CRÉATION
  // ============================================================

  /**
   * Création d'un contrat.
   *
   * Le bien immobilier n'est pas envoyé :
   * le contrat est directement lié à UniteLocativeId.
   *
   * Le statut initial est déterminé par le backend.
   */
  createContrat(
    contrat: CreerContratRequest
  ): Observable<ContratDto> {

    return this.http.post<ContratDto>(
      this.apiUrl,
      contrat
    );
  }


  // ============================================================
  // MODIFICATION
  // ============================================================

  /**
   * Modification d'un contrat.
   *
   * ModifierContratRequest ne contient volontairement pas :
   *
   * - uniteLocativeId
   * - locataireId
   * - bienImmobilierId
   *
   * Ces informations ne sont donc pas modifiables
   * par cette opération.
   */
  updateContrat(
    id: string,
    contrat: ModifierContratRequest
  ): Observable<void> {

    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      contrat
    );
  }


  // ============================================================
  // SUPPRESSION
  // ============================================================

  /**
   * Suppression logique du contrat.
   */
  deleteContrat(
    id: string
  ): Observable<{ message: string }> {

    return this.http.delete<{ message: string }>(
      `${this.apiUrl}/${id}`
    );
  }
}