import { Injectable, inject } from '@angular/core';
import {
  HttpClient,
  HttpParams
} from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment.development';


// ============================================================
// TYPES
// ============================================================

export interface EncaissementsGraphDto {
  labels: string[];
  datasets: number[];
}


export interface AlerteRapportDto {
  id: string;
  titre: string;
  message: string;
  severite: 'danger' | 'warning' | 'info';
  type: string;

  contratId?: string;

  uniteReference?: string;
  bienNom?: string;

  dateFin?: string;

  joursDepuisExpiration?: number;
  joursAvantExpiration?: number;

  montantLoyer?: number;
  montantPaye?: number;
  solde?: number;

  dateLimite?: string;
  joursDeRetard?: number;
}

export interface DerniereUniteDto {
  id: string;
  reference: string;
  type: number;
  superficie: number;
  loyer: number;
  statut: number;
  dateCreation: string;

  bien: {
    id: string;
    reference: string;
    nom: string;
  };
}


export interface ImpayeDto {
  contratId: string;

  uniteReference: string;

  bienNom: string;

  bienReference: string;

  locataireNom: string;

  locataireTelephone: string;

  montantLoyer: number;

  montantPaye: number;

  solde: number;

  delaiToleranceJours: number;

  dateLimite: string;

  joursDeRetard: number;
}


// ============================================================
// DASHBOARD KPI
// ============================================================

export interface DashboardKpiDto {

  totalBiens: number;

  totalUnites: number;

  unitesLouees: number;

  unitesDisponibles: number;

  tauxOccupation: number;

  encaissementsMois: number;

  encaissementsGraph: EncaissementsGraphDto;

  contratsActifs: number;

  contratsExpires: number;

  nombreImpayes: number;

  montantImpayes: number;

  alertes: AlerteRapportDto[];

  impayes: ImpayeDto[];

  dernieresUnites: DerniereUniteDto[];
}


// ============================================================
// ENCAISSEMENTS PAR MODE
// ============================================================

export interface EncaissementDetailDto {

  id: string;

  datePaiement: string;

  montant: number;

  modePaiement: string;

  uniteReference: string;

  bienNom: string;

  bienReference: string;

  locataireNom: string;
}


export interface EncaissementParModeDto {

  modePaiement: string;

  totalRecette: number;

  nombreTransactions: number;

  details: EncaissementDetailDto[];
}


export interface EncaissementsParModeResponseDto {

  dateDebut?: string;

  dateFin?: string;

  total: number;

  nombreTransactions: number;

  parMode: EncaissementParModeDto[];
}


// ============================================================
// HISTORIQUE LOCATAIRE
// ============================================================

export interface HistoriquePaiementDto {

  id: string;

  datePaiement: string;

  montant: number;

  statut: string;

  modePaiement: string;

  numeroQuittance: string;

  uniteReference: string;

  bienNom: string;

  bienReference: string;
}


export interface HistoriqueLocataireDto {

  locataire: {
    id: string;
    prenom: string;
    nom: string;
    email?: string;
    telephone?: string;
  };

  totalVersements: number;

  nombrePaiements: number;

  historique: HistoriquePaiementDto[];
}


// ============================================================
// RAPPORT CONTRATS
// ============================================================

export interface RapportContratDto {

  id: string;

  uniteReference: string;

  bienNom: string;

  bienReference: string;

  bienAdresse: string;

  locataireNom: string;

  dateDebut: string;

  dateFin: string;

  montantLoyer: number;

  montantCaution: number;

  statutContrat: string;

  estExpire: boolean;

  joursAvantExpiration: number;

  delaiJoursTolerance: number;
}


// ============================================================
// RAPPORT IMPAYÉS
// ============================================================

export interface RapportImpayesResponseDto {

  nombreImpayes: number;

  montantTotalImpayes: number;

  impayes: ImpayeDto[];
}


// ============================================================
// SERVICE
// ============================================================

@Injectable({
  providedIn: 'root'
})
export class RapportsService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    `${environment.apiUrl}/Rapports`;


  // ==========================================================
  // 1. DASHBOARD KPI
  // ==========================================================

  getDashboardKpi():
    Observable<DashboardKpiDto> {

    return this.http.get<DashboardKpiDto>(
      `${this.apiUrl}/dashboard-kpi`
    );
  }


  // ==========================================================
  // 2. ENCAISSEMENTS PAR MODE
  // ==========================================================

  getEncaissementsParMode(
    dateDebut?: string,
    dateFin?: string
  ): Observable<EncaissementsParModeResponseDto> {

    let params = new HttpParams();

    if (dateDebut) {
      params = params.set(
        'dateDebut',
        dateDebut
      );
    }

    if (dateFin) {
      params = params.set(
        'dateFin',
        dateFin
      );
    }

    return this.http.get<EncaissementsParModeResponseDto>(
      `${this.apiUrl}/encaissements-par-mode`,
      { params }
    );
  }


  // ==========================================================
  // 3. HISTORIQUE LOCATAIRE
  // ==========================================================

  getHistoriquePaiementsLocataire(
    locataireId: string
  ): Observable<HistoriqueLocataireDto> {

    return this.http.get<HistoriqueLocataireDto>(
      `${this.apiUrl}/historique-locataire/${locataireId}`
    );
  }


  // ==========================================================
  // 4. RAPPORT CONTRATS
  // ==========================================================

  getRapportContrats(
    statut?: string
  ): Observable<RapportContratDto[]> {

    let params = new HttpParams();

    if (statut) {
      params = params.set(
        'statut',
        statut
      );
    }

    return this.http.get<RapportContratDto[]>(
      `${this.apiUrl}/contrats`,
      { params }
    );
  }


  // ==========================================================
  // 5. RAPPORT IMPAYÉS
  // ==========================================================

  getRapportImpayes():
    Observable<RapportImpayesResponseDto> {

    return this.http.get<RapportImpayesResponseDto>(
      `${this.apiUrl}/impayes`
    );
  }


  // ==========================================================
  // 6. RAPPORT D'UNE SOCIÉTÉ
  // ==========================================================

  getRapportSociete(
    societeId: string
  ): Observable<any> {

    return this.http.get<any>(
      `${this.apiUrl}/societe/${societeId}`
    );
  }


  // ==========================================================
  // 7. RAPPORT D'UN BIEN
  // ==========================================================

  getRapportBien(
    bienImmobilierId: string
  ): Observable<any> {

    return this.http.get<any>(
      `${this.apiUrl}/bien/${bienImmobilierId}`
    );
  }


  // ==========================================================
  // 8. RAPPORT D'UNE UNITÉ
  // ==========================================================

  getRapportUnite(
    uniteLocativeId: string
  ): Observable<any> {

    return this.http.get<any>(
      `${this.apiUrl}/unite/${uniteLocativeId}`
    );
  }


  // ==========================================================
  // 9. EXPORT PDF
  // ==========================================================

  telechargerRapportPdf(
    typeRapport: string,
    titreRapport: string,
    donnees: unknown
  ): Observable<Blob> {

    const params =
      new HttpParams()
        .set(
          'typeRapport',
          typeRapport
        )
        .set(
          'titreRapport',
          titreRapport
        );

    return this.http.post(
      `${this.apiUrl}/export-pdf`,
      donnees,
      {
        params,
        responseType: 'blob'
      }
    );
  }
}