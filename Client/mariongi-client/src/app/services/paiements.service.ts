import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment.development';
import { ModePaiement, PaiementDto, PaiementListItem } from '../models/gestimmo.models';

export interface InitierPaiementRequest {
  contratId: string;
  montant: number;
  modePaiement: ModePaiement;
  telephone: string;
  moisLoyer: string;
}

export interface PaiementEspecesRequest {
  contratId: string;
  montant: number;
  moisLoyer: string;
}

export interface PaiementResponse {
  message: string;
  paiementId: string;
}

export interface StatutResponse {
  statut: 'EnAttente' | 'Confirme' | 'Echoue';
}

@Injectable({
  providedIn: 'root'
})
export class PaiementsService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl = `${environment.apiUrl}/Paiements`;

  // ============================================================
  // INITIER UN PAIEMENT EN LIGNE
  // ============================================================

  initierPaiement(
    request: InitierPaiementRequest
  ): Observable<PaiementResponse> {

    return this.http.post<PaiementResponse>(
      `${this.apiUrl}/initier`,
      request
    );
  }

  // ============================================================
  // ENREGISTRER UN PAIEMENT EN ESPÈCES
  // ============================================================

  enregistrerPaiementEspeces(
    request: PaiementEspecesRequest
  ): Observable<PaiementResponse> {

    return this.http.post<PaiementResponse>(
      `${this.apiUrl}/especes`,
      request
    );
  }

  // ============================================================
  // CONSULTER LE STATUT D'UN PAIEMENT
  // ============================================================

  getStatut(
    paiementId: string
  ): Observable<StatutResponse> {

    return this.http.get<StatutResponse>(
      `${this.apiUrl}/${paiementId}/statut`
    );
  }

  // ============================================================
  // TÉLÉCHARGER UNE QUITTANCE
  // ============================================================

  telechargerQuittanceBlob(
    paiementId: string
  ): void {

    this.http.get(
      `${this.apiUrl}/${paiementId}/quittance`,
      {
        responseType: 'blob'
      }
    ).subscribe({
      next: (blob) => {

        const url = window.URL.createObjectURL(blob);

        const a = document.createElement('a');

        a.href = url;
        a.download = `Quittance_${paiementId}.pdf`;

        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);

        window.URL.revokeObjectURL(url);
      },

      error: (err) => {
        console.error(
          'Erreur lors du téléchargement de la quittance :',
          err
        );
      }
    });
  }

  // ============================================================
  // RÉCUPÉRER LES PAIEMENTS
  // ============================================================


    getPaiements(): Observable<PaiementListItem[]> {
      return this.http.get<PaiementListItem[]>(
        `${this.apiUrl}`
      );
    }


  // ============================================================
  // TÉLÉCHARGER TOUTES LES QUITTANCES D'UN CONTRAT
  // ============================================================

  telechargerToutesLesQuittances(
    contratId: string
  ): Observable<Blob> {

    return this.http.get(
      `${this.apiUrl}/contrat/${contratId}/toutes-les-quittances`,
      {
        responseType: 'blob'
      }
    );
  }
}