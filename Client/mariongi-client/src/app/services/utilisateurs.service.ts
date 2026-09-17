import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map, catchError, of } from 'rxjs';
import { UtilisateurDto, RoleUtilisateur } from '../models/gestimmo.models';
import { environment } from '../../environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class UtilisateursService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/Utilisateurs`;

  // GET: /api/Utilisateurs
  getUtilisateurs(): Observable<UtilisateurDto[]> {
    return this.http.get<UtilisateurDto[]>(this.apiUrl).pipe(
      map(data => Array.isArray(data) ? data : []),
      catchError(err => {
        console.error('Erreur lors du chargement des utilisateurs:', err);
        return of([]);
      })
    );
  }

  // GET: /api/Utilisateurs (filtré pour les locataires)
  getLocataires(): Observable<UtilisateurDto[]> {
    return this.getUtilisateurs().pipe(
      map(utilisateurs => {
        const liste = utilisateurs ?? [];
        return liste.filter(u => 
          u.role === RoleUtilisateur.Locataire ||
          String(u.role).toLowerCase() === 'locataire' ||
          u.role === 3
        );
      })
    );
  }

  // GET: /api/Utilisateurs/{id}
  getUtilisateurById(id: string): Observable<UtilisateurDto> {
    return this.http.get<UtilisateurDto>(`${this.apiUrl}/${id}`);
  }

  // POST: /api/Utilisateurs?motDePasseInitial=...
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