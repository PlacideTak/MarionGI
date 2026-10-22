import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';

import {
  DeclarationFiscaleDto,
  DeclarationFiscaleRequest,
  BienDeclarationFiscaleDto
} from '../models/gestimmo.models';

@Injectable({
  providedIn: 'root'
})
export class DeclarationsFiscalesService {
  private readonly apiUrl =
    `${environment.apiUrl}/DeclarationsFiscales`;

  private readonly biensUrl =
    `${environment.apiUrl}/BiensImmobiliers`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<DeclarationFiscaleDto[]> {
    return this.http.get<DeclarationFiscaleDto[]>(this.apiUrl);
  }

  getById(id: string): Observable<DeclarationFiscaleDto> {
    return this.http.get<DeclarationFiscaleDto>(
      `${this.apiUrl}/${id}`
    );
  }

  create(
    request: DeclarationFiscaleRequest
  ): Observable<unknown> {
    return this.http.post(this.apiUrl, request);
  }

  update(
    id: string,
    request: DeclarationFiscaleRequest
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      { ...request, id }
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getBiens(): Observable<BienDeclarationFiscaleDto[]> {
  return this.http.get<BienDeclarationFiscaleDto[]>(
    this.biensUrl
  );
}
}
