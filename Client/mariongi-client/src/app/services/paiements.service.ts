import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';
import { ModePaiement, PaiementDto } from '../models/gestimmo.models';

export interface InitierPaiementRequest {
  contratId: string;
  montant: number;
  modePaiement: ModePaiement;
  telephone: string;
}

export interface PaiementEspecesRequest {
  contratId: string;
  montant: number;
}

export interface PaiementResponse {
  message: string;
  paiementId: string;
}

export interface StatutResponse {
  statut: 'EnAttente' | 'Confirme' | 'Echoue';
}

@Injectable({ providedIn: 'root' })
export class PaiementsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/Paiements`;

  initierPaiement(request: InitierPaiementRequest): Observable<PaiementResponse> {
    return this.http.post<PaiementResponse>(`${this.apiUrl}/initier`, request);
  }

  enregistrerPaiementEspeces(request: PaiementEspecesRequest): Observable<PaiementResponse> {
    return this.http.post<PaiementResponse>(`${this.apiUrl}/especes`, request);
  }

  getStatut(paiementId: string): Observable<StatutResponse> {
    return this.http.get<StatutResponse>(`${this.apiUrl}/${paiementId}/statut`);
  }

  telechargerQuittanceBlob(paiementId: string): void {
    this.http.get(`${this.apiUrl}/${paiementId}/quittance`, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Quittance_${paiementId}.pdf`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => console.error('Erreur téléchargement quittance:', err)
    });
  }

  getPaiements(): Observable<PaiementDto[]> {
     return this.http.get<PaiementDto[]>(`${this.apiUrl}/Paiements`);
  }

  telechargerToutesLesQuittances(contratId: string): Observable<Blob> {
  return this.http.get(`${this.apiUrl}/contrat/${contratId}/toutes-les-quittances`, { 
    responseType: 'blob' 
  });
}


}