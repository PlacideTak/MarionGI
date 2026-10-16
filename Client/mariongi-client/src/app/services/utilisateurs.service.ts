
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';

import {
  UtilisateurDto,
  RoleUtilisateur,
  CreerUtilisateurRequest,
  ModifierUtilisateurRequest,
  ModifierStatutUtilisateurRequest
} from '../models/gestimmo.models';

import { environment } from '../../environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class UtilisateursService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    `${environment.apiUrl}/Utilisateurs`;

  // GET : api/Utilisateurs
  getUtilisateurs(): Observable<UtilisateurDto[]> {
    return this.http.get<UtilisateurDto[]>(this.apiUrl).pipe(
      map(data => Array.isArray(data) ? data : [])
    );
  }

  // Filtrage local des locataires parmi les utilisateurs visibles.
  getLocataires(): Observable<UtilisateurDto[]> {
    return this.getUtilisateurs().pipe(
      map(utilisateurs =>
        utilisateurs.filter(
          utilisateur =>
            utilisateur.role === RoleUtilisateur.Locataire
        )
      )
    );
  }

  // GET : api/Utilisateurs/{id}
  getUtilisateurById(id: string): Observable<UtilisateurDto> {
    return this.http.get<UtilisateurDto>(
      `${this.apiUrl}/${id}`
    );
  }

  // POST : api/Utilisateurs
  createUtilisateur(
    utilisateur: CreerUtilisateurRequest
  ): Observable<UtilisateurDto> {
    return this.http.post<UtilisateurDto>(
      this.apiUrl,
      utilisateur
    );
  }

  // PUT : api/Utilisateurs/{id}
  updateUtilisateur(
    id: string,
    utilisateur: ModifierUtilisateurRequest
  ): Observable<UtilisateurDto> {
    return this.http.put<UtilisateurDto>(
      `${this.apiUrl}/${id}`,
      utilisateur
    );
  }

  // PATCH : api/Utilisateurs/{id}/statut
  updateStatut(
    id: string,
    statut: boolean
  ): Observable<void> {
    const request: ModifierStatutUtilisateurRequest = {
      statut
    };

    return this.http.patch<void>(
      `${this.apiUrl}/${id}/statut`,
      request
    );
  }

  // DELETE : api/Utilisateurs/{id}
  deleteUtilisateur(id: string): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }
}
