import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment.development';
import { ChargeBienDto, CreerChargeBienRequest, FiltreChargesBiens, ModifierChargeBienRequest, SupprimerChargeBienResponse, TypeCharge } from '../models/gestimmo.models';

// ============================================================
// SERVICE
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
    {
      label: 'Taxe foncière',
      value: TypeCharge.TaxeFonciere
    },
    {
      label: 'Entretien et réparations',
      value: TypeCharge.Entretien
    },
    {
      label: 'Assurance',
      value: TypeCharge.Assurance
    },
    {
      label: 'Intérêts d’emprunt',
      value: TypeCharge.InteretEmprunt
    }
  ];

  // ==========================================================
  // LIBELLÉ DU TYPE DE CHARGE
  // ==========================================================

  getTypeChargeLibelle(type: TypeCharge): string {
    const charge = this.typesCharges.find(
      t => t.value === type
    );

    return charge?.label ?? 'Inconnu';
  }

  // ==========================================================
  // GET : LISTE DES CHARGES
  //
  // GET /api/ChargesBiens
  // Filtres facultatifs : bien, unité, type, dates
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
  //
  // GET /api/ChargesBiens/{id}
  // ==========================================================

  getChargeById(
    id: string
  ): Observable<ChargeBienDto> {

    return this.http.get<ChargeBienDto>(
      `${this.apiUrl}/${id}`
    );
  }

  // ==========================================================
  // POST : CRÉER UNE CHARGE
  //
  // POST /api/ChargesBiens
  // Le contrôleur reçoit un objet JSON.
  // ==========================================================

  createCharge(
    request: CreerChargeBienRequest
  ): Observable<ChargeBienDto> {

    return this.http.post<ChargeBienDto>(
      this.apiUrl,
      request
    );
  }

  // ==========================================================
  // PUT : MODIFIER UNE CHARGE
  //
  // PUT /api/ChargesBiens/{id}
  // ==========================================================

  updateCharge(
    id: string,
    request: ModifierChargeBienRequest
  ): Observable<void> {

    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      request
    );
  }

  // ==========================================================
  // DELETE : SUPPRESSION LOGIQUE
  //
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

