import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DemandeVisiteDto } from '../models/gestimmo.models';
import { environment } from '../../environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class DemandesVisiteService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/DemandesVisite`;

  getDemandesVisite(): Observable<DemandeVisiteDto[]> {
    return this.http.get<DemandeVisiteDto[]>(this.apiUrl);
  }

  createDemandeVisite(demande: any): Observable<DemandeVisiteDto> {
    return this.http.post<DemandeVisiteDto>(this.apiUrl, demande);
  }

  updateDemandeVisite(id: string, demande: DemandeVisiteDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, demande);
  }

  deleteDemandeVisite(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}