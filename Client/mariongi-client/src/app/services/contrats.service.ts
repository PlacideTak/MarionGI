import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ContratDto, StatutContrat } from '../models/gestimmo.models';
import { environment } from '../../environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class ContratsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/Contrats`;

  getContrats(statut?: StatutContrat): Observable<ContratDto[]> {
    let params = new HttpParams();
    if (statut !== undefined && statut !== null) {
      params = params.set('statut', statut.toString());
    }
    return this.http.get<ContratDto[]>(this.apiUrl, { params });
  }

  getContratById(id: string): Observable<ContratDto> {
    return this.http.get<ContratDto>(`${this.apiUrl}/${id}`);
  }

  createContrat(contrat: Partial<ContratDto>): Observable<ContratDto> {
    return this.http.post<ContratDto>(this.apiUrl, contrat);
  }

  updateContrat(id: string, contrat: ContratDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, contrat);
  }

  deleteContrat(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }
}