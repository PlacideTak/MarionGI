
import { Injectable, inject } from '@angular/core';
import {
  HttpClient,
  HttpParams
} from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment.development';

import {
  TicketIncidentDto,
  TicketIncidentDetailDto,
  ModifierTicketIncidentRequest,
  ModifierStatutTicketRequest,
  ValiderDevisTicketRequest,
  CreerTicketIntervenantRequest,
  TicketIncidentFilterCriteria,
  TypePhotoIncident,
  LocataireOption,
  ModifierTicketIntervenantRequest
} from '../models/gestimmo.models';

@Injectable({
  providedIn: 'root'
})
export class TicketsService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl = `${environment.apiUrl}/Tickets`;

  private readonly serverUrl = environment.apiUrl.replace(/\/api\/?$/, '');

  /**
   * Liste les tickets avec filtres facultatifs.
   */
  obtenirTous(
    filtres?: TicketIncidentFilterCriteria
  ): Observable<TicketIncidentDto[]> {
    let params = new HttpParams();

    if (filtres?.statut !== null && filtres?.statut !== undefined) {
      params = params.set('statut', String(filtres.statut));
    }

    if (filtres?.priorite !== null && filtres?.priorite !== undefined) {
      params = params.set('priorite', String(filtres.priorite));
    }

    if (filtres?.categorie !== null && filtres?.categorie !== undefined) {
      params = params.set('categorie', String(filtres.categorie));
    }

    if (filtres?.uniteLocativeId) {
      params = params.set('uniteLocativeId', filtres.uniteLocativeId);
    }

    return this.http.get<TicketIncidentDto[]>(this.apiUrl, { params });
  }

  /**
   * Obtient le détail d'un ticket.
   */
  obtenirParId(id: string): Observable<TicketIncidentDetailDto> {
    return this.http.get<TicketIncidentDetailDto>(
      `${this.apiUrl}/${id}`
    );
  }

  /**
   * Crée un ticket à partir d'un FormData.
   * Les photos sont transmises avec le champ multipart "Photos".
   */
  creer(formData: FormData): Observable<TicketIncidentDetailDto> {
    return this.http.post<TicketIncidentDetailDto>(
      this.apiUrl,
      formData
    );
  }

  /**
   * Modifie les informations d'un ticket existant.
   */
  modifier(
    id: string,
    request: ModifierTicketIncidentRequest
  ): Observable<TicketIncidentDetailDto> {
    return this.http.put<TicketIncidentDetailDto>(
      `${this.apiUrl}/${id}`,
      request
    );
  }


  supprimer(id: string): Observable<unknown> {
  return this.http.delete(`${this.apiUrl}/${id}`);
}

  /**
   * Modifie le statut d'un ticket.
   */
  modifierStatut(
    id: string,
    request: ModifierStatutTicketRequest
  ): Observable<unknown> {
    return this.http.patch(
      `${this.apiUrl}/${id}/statut`,
      request
    );
  }

  /**
   * Valide le devis associé au ticket.
   */
  validerDevis(
    id: string,
    request: ValiderDevisTicketRequest
  ): Observable<unknown> {
    return this.http.post(
      `${this.apiUrl}/${id}/valider-devis`,
      request
    );
  }

  /**
   * Clôture un ticket déjà résolu.
   */
  cloturer(id: string): Observable<unknown> {
    return this.http.post(
      `${this.apiUrl}/${id}/cloturer`,
      {}
    );
  }

  /**
   * Ajoute un intervenant au ticket.
   */
  ajouterIntervenant(
    id: string,
    request: CreerTicketIntervenantRequest
  ): Observable<unknown> {
    return this.http.post(
      `${this.apiUrl}/${id}/intervenants`,
      request
    );
  }

  /**
   * Ajoute des photos à un ticket existant.
   *
   * Attention : le contrôleur attend "Type" et "Photos",
   * pas "Fichiers".
   */
  ajouterPhotos(
    id: string,
    fichiers: File[],
    type: TypePhotoIncident
  ): Observable<unknown> {
    const formData = new FormData();

    formData.append('Type', String(type));

    for (const fichier of fichiers) {
      formData.append('Photos', fichier, fichier.name);
    }

    return this.http.post(
      `${this.apiUrl}/${id}/photos`,
      formData
    );
  }

  /**
   * Construit une URL utilisable pour afficher une photo.
   */

getPhotoUrl(urlPhoto: string | null | undefined): string {
  if (!urlPhoto?.trim()) {
    return '';
  }

  const url = urlPhoto.trim();

  // URL complète : ne pas la modifier.
  if (/^https?:\/\//i.test(url)) {
    return url;
  }

  // Éviter de préfixer deux fois le chemin.
  const chemin = url.startsWith('/') ? url : `/${url}`;

  return `${this.serverUrl.replace(/\/+$/, '')}${chemin}`;
}


  /**
 * Modifie un intervenant associé à un ticket.
 */
modifierIntervenant(
  ticketId: string,
  intervenantId: string,
  request: ModifierTicketIntervenantRequest
): Observable<unknown> {
  return this.http.put(
    `${this.apiUrl}/${ticketId}/intervenants/${intervenantId}`,
    request
  );
}

/**
 * Supprime un intervenant associé à un ticket.
 */
supprimerIntervenant(
  ticketId: string,
  intervenantId: string
): Observable<unknown> {
  return this.http.delete(
    `${this.apiUrl}/${ticketId}/intervenants/${intervenantId}`
  );
}

  obtenirLocatairesParUnite(
  uniteLocativeId: string
): Observable<LocataireOption[]> {
  return this.http.get<LocataireOption[]>(
    `${this.apiUrl}/locataires-par-unite/${uniteLocativeId}`
  );
}

}
