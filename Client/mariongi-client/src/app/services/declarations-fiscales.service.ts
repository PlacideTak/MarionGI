
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment.development';

import {
  DeclarationFiscaleDto,
  DeclarationFiscaleRequest,
  BienDeclarationFiscaleDto,
  CalculFiscalResponse
} from '../models/gestimmo.models';


@Injectable({
  providedIn: 'root'
})
export class DeclarationsFiscalesService {

  private readonly apiUrl =
    `${environment.apiUrl}/DeclarationsFiscales`;

  private readonly biensUrl =
    `${environment.apiUrl}/BiensImmobiliers`;

  constructor(
    private readonly http: HttpClient
  ) {}

  // ============================================================
  // LISTE DES DÉCLARATIONS
  // ============================================================

  getAll(): Observable<DeclarationFiscaleDto[]> {
    return this.http.get<DeclarationFiscaleDto[]>(
      this.apiUrl
    );
  }

  // ============================================================
  // DÉTAIL D'UNE DÉCLARATION
  // ============================================================

  getById(
    id: string
  ): Observable<DeclarationFiscaleDto> {
    return this.http.get<DeclarationFiscaleDto>(
      `${this.apiUrl}/${id}`
    );
  }

  // ============================================================
  // CRÉATION
  // ============================================================

  create(
    request: DeclarationFiscaleRequest
  ): Observable<unknown> {
    return this.http.post(
      this.apiUrl,
      request
    );
  }

  // ============================================================
  // MODIFICATION
  // ============================================================

  update(
    id: string,
    request: DeclarationFiscaleRequest
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      { ...request, id }
    );
  }

  // ============================================================
  // SUPPRESSION
  // ============================================================

  delete(
    id: string
  ): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }

  // ============================================================
  // CALCUL FISCAL
  // POST : api/DeclarationsFiscales/{id}/calculer
  // ============================================================

  calculer(
    id: string
  ): Observable<CalculFiscalResponse> {
    return this.http.post<CalculFiscalResponse>(
      `${this.apiUrl}/${id}/calculer`,
      {}
    );
  }

  // ============================================================
  // BIENS IMMOBILIERS
  // ============================================================

  getBiens(): Observable<BienDeclarationFiscaleDto[]> {
    return this.http.get<BienDeclarationFiscaleDto[]>(
      this.biensUrl
    );
  }
}
