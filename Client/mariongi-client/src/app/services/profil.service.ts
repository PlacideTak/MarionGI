import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class ProfilService {
  private apiUrl = `${environment.apiUrl}/profil`; // ex: https://localhost:7232/api/profil

  constructor(private http: HttpClient) {}

  getProfil(): Observable<any> {
    return this.http.get<any>(this.apiUrl);
  }

  modifierProfil(data: { nom: string; prenom: string; ancienMotDePasse?: string; nouveauMotDePasse?: string }): Observable<any> {
    return this.http.put<any>(this.apiUrl, data);
  }
}