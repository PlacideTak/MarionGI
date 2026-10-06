import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  SocieteDto,
  ModifierSocieteRequest,
  ModifierStatutSocieteRequest
} from '../models/gestimmo.models';

import { environment } from '../../environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class SocietesService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl = `${environment.apiUrl}/Societes`;

  // GET : /api/Societes
  // Administrateur : toutes les sociétés
  // Gestionnaire : uniquement sa société
  getSocietes(): Observable<SocieteDto[]> {
    return this.http.get<SocieteDto[]>(this.apiUrl);
  }

  // GET : /api/Societes/{id}
  getSocieteById(id: string): Observable<SocieteDto> {
    return this.http.get<SocieteDto>(
      `${this.apiUrl}/${id}`
    );
  }

  // POST : /api/Societes
  // Administrateur uniquement
  createSociete(
    societe: ModifierSocieteRequest
  ): Observable<SocieteDto> {
    return this.http.post<SocieteDto>(
      this.apiUrl,
      societe
    );
  }

  // PUT : /api/Societes/{id}
  updateSociete(
    id: string,
    societe: ModifierSocieteRequest
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      societe
    );
  }

  // PATCH : /api/Societes/{id}/statut
  updateStatut(
    id: string,
    request: ModifierStatutSocieteRequest
  ): Observable<any> {
    return this.http.patch<any>(
      `${this.apiUrl}/${id}/statut`,
      request
    );
  }

  // DELETE : /api/Societes/{id}
  // Administrateur uniquement
  deleteSociete(id: string): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }
}