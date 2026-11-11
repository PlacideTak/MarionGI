import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment.development';

import {
  OptionDeductionFiscaleDto,
  CreerOptionDeductionFiscaleRequest,
  ModifierOptionDeductionFiscaleRequest
} from '../models/gestimmo.models';

@Injectable({
  providedIn: 'root'
})
export class OptionsDeductionFiscalesService {

  private readonly apiUrl =
    `${environment.apiUrl}/OptionsDeductionFiscales`;

  constructor(
    private readonly http: HttpClient
  ) {}

  // ============================================================
  // LISTE DES OPTIONS DE DÉDUCTION
  // GET : api/OptionsDeductionFiscales?anneeFiscale=2026
  // ============================================================

  getAll(
    anneeFiscale?: number
  ): Observable<OptionDeductionFiscaleDto[]> {

    let params = new HttpParams();

    if (anneeFiscale !== undefined) {
      params = params.set(
        'anneeFiscale',
        anneeFiscale.toString()
      );
    }

    return this.http.get<OptionDeductionFiscaleDto[]>(
      this.apiUrl,
      { params }
    );
  }

  // ============================================================
  // DÉTAIL D'UNE OPTION
  // GET : api/OptionsDeductionFiscales/{id}
  // ============================================================

  getById(
    id: string
  ): Observable<OptionDeductionFiscaleDto> {

    return this.http.get<OptionDeductionFiscaleDto>(
      `${this.apiUrl}/${id}`
    );
  }

  // ============================================================
  // CRÉATION
  // POST : api/OptionsDeductionFiscales
  // ============================================================

  create(
    request: CreerOptionDeductionFiscaleRequest
  ): Observable<OptionDeductionFiscaleDto> {

    return this.http.post<OptionDeductionFiscaleDto>(
      this.apiUrl,
      request
    );
  }

  // ============================================================
  // MODIFICATION
  // PUT : api/OptionsDeductionFiscales/{id}
  // ============================================================

  update(
    id: string,
    request: ModifierOptionDeductionFiscaleRequest
  ): Observable<OptionDeductionFiscaleDto> {

    return this.http.put<OptionDeductionFiscaleDto>(
      `${this.apiUrl}/${id}`,
      request
    );
  }

  // ============================================================
  // VERROUILLAGE
  // POST : api/OptionsDeductionFiscales/{id}/verrouiller
  // ============================================================

  verrouiller(
    id: string
  ): Observable<unknown> {

    return this.http.post(
      `${this.apiUrl}/${id}/verrouiller`,
      {}
    );
  }

  // ============================================================
  // SUPPRESSION
  // DELETE : api/OptionsDeductionFiscales/{id}
  // ============================================================

  delete(
    id: string
  ): Observable<void> {

    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }
}
