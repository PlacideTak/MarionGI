import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';

export interface DashboardKpiDto {
  biensDisponibles: number;
  encaissementsMois: number; 
  impayesEnCours: number;
  tauxOccupation: number;
  alertes?: any[];
  encaissementsGraph?: {
    labels: string[];
    datasets: number[];
  };
}

@Injectable({
  providedIn: 'root'
})
export class RapportsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/Rapports`;

  // GET: /api/Rapports/dashboard-kpi
  getDashboardKpi(): Observable<DashboardKpiDto> {
    return this.http.get<DashboardKpiDto>(`${this.apiUrl}/dashboard-kpi`);
  }

  // 1. Liste des encaissements périodiques regroupés par mode de paiement
  getEncaissementsParMode(dateDebut?: string, dateFin?: string): Observable<any> {
    let params = new HttpParams();
    if (dateDebut) params = params.set('dateDebut', dateDebut);
    if (dateFin) params = params.set('dateFin', dateFin);

    return this.http.get<any>(`${this.apiUrl}/rapports/encaissements-par-mode`, { params });
  }

  // 2. Historique des paiements d'un locataire spécifique
  getHistoriquePaiementsLocataire(locataireId: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/rapports/historique-locataire/${locataireId}`);
  }

  // 3. Tableau de tous les contrats périodiques
  getRapportContrats(statut?: string): Observable<any> {
    let params = new HttpParams();
    if (statut) params = params.set('statut', statut);

    return this.http.get<any>(`${this.apiUrl}/rapports/contrats`, { params });
  }

  // 4. Liste des biens dont le loyer n'a pas été payé après le délai de tolérance
  getRapportImpayes(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/rapports/impayes`);
  }

  telechargerRapportPdf(typeRapport: string, titreRapport: string, donnees: any): Observable<Blob> {
  const url = `${this.apiUrl}/export-pdf?typeRapport=${typeRapport}&titreRapport=${encodeURIComponent(titreRapport)}`;
  return this.http.post(url, donnees, { responseType: 'blob' });
}
}