import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  DemandeVisiteDto,
  StatutDemandeVisite
} from '../models/gestimmo.models';

import { environment } from '../../environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class DemandesVisiteService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    `${environment.apiUrl}/DemandesVisite`;


  // =========================================================
  // RÉCUPÉRER LES DEMANDES
  // =========================================================

  getDemandesVisite(): Observable<DemandeVisiteDto[]> {
    return this.http.get<DemandeVisiteDto[]>(
      this.apiUrl
    );
  }


  // =========================================================
  // CRÉER UNE DEMANDE
  // =========================================================

  createDemandeVisite(
    demande: DemandeVisiteDto
  ): Observable<DemandeVisiteDto> {

    return this.http.post<DemandeVisiteDto>(
      this.apiUrl,
      demande
    );
  }


  // =========================================================
  // MODIFIER UNE DEMANDE
  // =========================================================

  updateDemandeVisite(
    id: string,
    demande: DemandeVisiteDto
  ): Observable<void> {

    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      demande
    );
  }


  // =========================================================
  // SUPPRIMER UNE DEMANDE
  // =========================================================

  deleteDemandeVisite(
    id: string
  ): Observable<void> {

    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }


  // =========================================================
  // LIBELLÉ DU STATUT
  // =========================================================

  getStatutLibelle(
    statut: StatutDemandeVisite | number
  ): string {

    switch (statut) {

      case StatutDemandeVisite.EnAttente:
        return 'En attente';

      case StatutDemandeVisite.Confirmee:
        return 'Confirmée';

      case StatutDemandeVisite.Annulee:
        return 'Annulée';

      case StatutDemandeVisite.Effectuee:
        return 'Effectuée';

      default:
        return 'Inconnu';
    }
  }


  // =========================================================
  // CLASSE CSS DU STATUT
  // =========================================================

  getStatutClass(
    statut: StatutDemandeVisite | number
  ): string {

    switch (statut) {

      case StatutDemandeVisite.EnAttente:
        return 'statut-en-attente';

      case StatutDemandeVisite.Confirmee:
        return 'statut-confirmee';

      case StatutDemandeVisite.Annulee:
        return 'statut-annulee';

      case StatutDemandeVisite.Effectuee:
        return 'statut-effectuee';

      default:
        return 'statut-inconnu';
    }
  }

}