import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment.development';

import {
  ChargeBienDto,
  ChargeBienOperationResponse,
  CreerChargeBienRequest,
  FiltreChargesBiens,
  ModifierChargeBienRequest,
  SupprimerChargeBienResponse,
  TypeCharge,
  StatutDeductibilite
} from '../models/gestimmo.models';

// ============================================================
// SERVICE DE GESTION DES CHARGES IMMOBILIÈRES
// ============================================================

@Injectable({
  providedIn: 'root'
})
export class ChargesBiensService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    `${environment.apiUrl}/ChargesBiens`;

  // ==========================================================
  // RÉFÉRENTIEL : TYPES DE CHARGES
  // ==========================================================

  readonly typesCharges = [
    { label: 'Taxe foncière', value: TypeCharge.TaxeFonciere },
    { label: 'Entretien et réparations', value: TypeCharge.Entretien },
    { label: 'Assurance', value: TypeCharge.Assurance },
    { label: 'Intérêts d’emprunt', value: TypeCharge.InteretEmprunt }
  ];

  // ==========================================================
  // RÉFÉRENTIEL : STATUTS DE DÉDUCTIBILITÉ
  // ==========================================================

  readonly statutsDeductibilite = [
    {
      label: 'À qualifier',
      value: StatutDeductibilite.AQualifier
    },
    {
      label: 'Déductible',
      value: StatutDeductibilite.Deductible
    },
    {
      label: 'Non déductible',
      value: StatutDeductibilite.NonDeductible
    },
    {
      label: 'Partiellement déductible',
      value: StatutDeductibilite.PartiellementDeductible
    }
  ];

  // ==========================================================
  // LIBELLÉ DU TYPE DE CHARGE
  // ==========================================================

  getTypeChargeLibelle(type: TypeCharge): string {
    return this.typesCharges.find(
      t => t.value === type
    )?.label ?? 'Inconnu';
  }

  // ==========================================================
  // LIBELLÉ DU STATUT DE DÉDUCTIBILITÉ
  // ==========================================================

  getStatutDeductibiliteLibelle(
    statut: StatutDeductibilite
  ): string {
    return this.statutsDeductibilite.find(
      s => s.value === statut
    )?.label ?? 'Inconnu';
  }

  // ==========================================================
  // GET : LISTE DES CHARGES
  // GET /api/ChargesBiens
  // ==========================================================

  getCharges(
    filtres?: FiltreChargesBiens
  ): Observable<ChargeBienDto[]> {

    let params = new HttpParams();

    if (filtres?.bienImmobilierId) {
      params = params.set(
        'bienImmobilierId',
        filtres.bienImmobilierId
      );
    }

    if (filtres?.uniteLocativeId) {
      params = params.set(
        'uniteLocativeId',
        filtres.uniteLocativeId
      );
    }

    if (filtres?.typeCharge != null) {
      params = params.set(
        'typeCharge',
        filtres.typeCharge.toString()
      );
    }

    if (filtres?.dateDebut) {
      params = params.set(
        'dateDebut',
        filtres.dateDebut
      );
    }

    if (filtres?.dateFin) {
      params = params.set(
        'dateFin',
        filtres.dateFin
      );
    }

    return this.http.get<ChargeBienDto[]>(
      this.apiUrl,
      { params }
    );
  }

  // ==========================================================
  // GET : DÉTAIL D'UNE CHARGE
  // GET /api/ChargesBiens/{id}
  // ==========================================================

  getChargeById(id: string): Observable<ChargeBienDto> {
    return this.http.get<ChargeBienDto>(
      `${this.apiUrl}/${id}`
    );
  }

  // ==========================================================
  // POST : CRÉER UNE CHARGE
  // POST /api/ChargesBiens
  // ==========================================================

  createCharge(
    request: CreerChargeBienRequest
  ): Observable<ChargeBienOperationResponse> {

    return this.http.post<ChargeBienOperationResponse>(
      this.apiUrl,
      request
    );
  }

  // ==========================================================
  // PUT : MODIFIER UNE CHARGE
  // PUT /api/ChargesBiens/{id}
  // ==========================================================

  updateCharge(
    id: string,
    request: ModifierChargeBienRequest
  ): Observable<ChargeBienOperationResponse> {

    return this.http.put<ChargeBienOperationResponse>(
      `${this.apiUrl}/${id}`,
      request
    );
  }


  qualifierCharge(
  id: string,
  request: {
    statutDeductibilite: StatutDeductibilite;
    montantDeductibleFiscalement: number | null;
    justificationNonDeductibilite: string | null;
  }
): Observable<any> {
  return this.http.put<any>(
    `${this.apiUrl}/${id}/qualification-fiscale`,
    request
  );
}

  // ==========================================================
  // DELETE : SUPPRESSION LOGIQUE
  // DELETE /api/ChargesBiens/{id}
  // ==========================================================

  deleteCharge(
    id: string
  ): Observable<SupprimerChargeBienResponse> {

    return this.http.delete<SupprimerChargeBienResponse>(
      `${this.apiUrl}/${id}`
    );
  }
}