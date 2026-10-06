import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';

import {
  UtilisateurDto,
  RoleUtilisateur,
  CreerUtilisateurRequest,
  ModifierUtilisateurRequest
} from '../models/gestimmo.models';

import { environment } from '../../environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class UtilisateursService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    `${environment.apiUrl}/Utilisateurs`;


  // ============================================================
  // GET : /api/Utilisateurs
  // ============================================================

  getUtilisateurs(): Observable<UtilisateurDto[]> {

    return this.http
      .get<UtilisateurDto[]>(this.apiUrl)
      .pipe(
        map(data => Array.isArray(data) ? data : [])
      );
  }


  // ============================================================
  // GET : /api/Utilisateurs
  // Filtre les locataires
  // ============================================================

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


  // ============================================================
  // GET : /api/Utilisateurs/{id}
  // ============================================================

  getUtilisateurById(
    id: string
  ): Observable<UtilisateurDto> {

    return this.http.get<UtilisateurDto>(
      `${this.apiUrl}/${id}`
    );
  }


  // ============================================================
  // POST : /api/Utilisateurs
  // ============================================================

  createUtilisateur(
    utilisateur: CreerUtilisateurRequest
  ): Observable<UtilisateurDto> {

    return this.http.post<UtilisateurDto>(
      this.apiUrl,
      utilisateur
    );
  }


  // ============================================================
  // PUT : /api/Utilisateurs/{id}
  // ============================================================

  updateUtilisateur(
    id: string,
    utilisateur: ModifierUtilisateurRequest
  ): Observable<void> {

    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      utilisateur
    );
  }


  // ============================================================
  // PATCH : /api/Utilisateurs/{id}/statut
  // ============================================================

  updateStatut(
    id: string,
    statut: boolean
  ): Observable<any> {

    return this.http.patch<any>(
      `${this.apiUrl}/${id}/statut`,
      {
        statut
      }
    );
  }


  // ============================================================
  // DELETE : /api/Utilisateurs/{id}
  // ============================================================

  deleteUtilisateur(
    id: string
  ): Observable<void> {

    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }
}