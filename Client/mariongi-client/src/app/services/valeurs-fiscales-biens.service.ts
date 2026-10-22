import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment.development';

import {
  TypeValeurFiscale,
  ValeurFiscaleBienDto,
  CreerValeurFiscaleBienRequest,
  ModifierValeurFiscaleBienRequest
} from '../models/gestimmo.models';

@Injectable({
  providedIn: 'root'
})
export class ValeursFiscalesBiensService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    `${environment.apiUrl}/ValeursFiscalesBiens`;

  getAll(
    bienImmobilierId?: string,
    anneeFiscale?: number,
    typeValeur?: TypeValeurFiscale
  ): Observable<ValeurFiscaleBienDto[]> {
    let params = new HttpParams();

    if (bienImmobilierId) {
      params = params.set('bienImmobilierId', bienImmobilierId);
    }

    if (anneeFiscale !== undefined) {
      params = params.set('anneeFiscale', anneeFiscale);
    }

    if (typeValeur !== undefined) {
      params = params.set('typeValeur', typeValeur);
    }

    return this.http.get<ValeurFiscaleBienDto[]>(
      this.apiUrl,
      { params }
    );
  }

  getById(id: string): Observable<ValeurFiscaleBienDto> {
    return this.http.get<ValeurFiscaleBienDto>(
      `${this.apiUrl}/${id}`
    );
  }

  create(
    request: CreerValeurFiscaleBienRequest
  ): Observable<unknown> {
    return this.http.post(this.apiUrl, request);
  }

  update(
    id: string,
    request: ModifierValeurFiscaleBienRequest
  ): Observable<unknown> {
    return this.http.put(`${this.apiUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}