import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UtilisateurDto } from '../models/gestimmo.models';
import { environment } from '../../environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class UtilisateursService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/Utilisateurs`;

  // GET: /api/Utilisateurs
  getUtilisateurs(): Observable<UtilisateurDto[]> {
    return this.http.get<UtilisateurDto[]>(this.apiUrl);
  }

  // GET: /api/Utilisateurs/{id}
  getUtilisateurById(id: string): Observable<UtilisateurDto> {
    return this.http.get<UtilisateurDto>(`${this.apiUrl}/${id}`);
  }

  // POST: /api/Utilisateurs?motDePasseInitial=...
  // ⚠️ Le contrôleur attend motDePasseInitial en query string ([FromQuery]),
  // pas dans le corps JSON — d'où le paramètre séparé ici.
  createUtilisateur(
    utilisateur: Omit<UtilisateurDto, 'id' | 'dateCreation'>,
    motDePasseInitial: string
  ): Observable<UtilisateurDto> {
    const params = new HttpParams().set('motDePasseInitial', motDePasseInitial);
    return this.http.post<UtilisateurDto>(this.apiUrl, utilisateur, { params });
  }

  // PUT: /api/Utilisateurs/{id}
  updateUtilisateur(id: string, utilisateur: UtilisateurDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, utilisateur);
  }

  // DELETE: /api/Utilisateurs/{id}
  deleteUtilisateur(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}