import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  BienDto,
  TypeBien,
  CreerBienRequest,
  ModifierBienRequest,
  UniteLocativeDto
} from '../models/gestimmo.models';

import { environment } from '../../environments/environment.development';


// ============================================================
// TYPES DE RÉPONSE API
// ============================================================

export interface BienDetailDto extends BienDto {
  unitesLocatives: UniteLocativeDto[];
}

export interface SupprimerBienResponse {
  message: string;
}


@Injectable({
  providedIn: 'root'
})
export class BiensService {

  private readonly http = inject(HttpClient);


  // ==========================================================
  // URL API
  // ==========================================================

  private readonly apiUrl =
    `${environment.apiUrl}/BienImmobilier`;


  // ==========================================================
  // URL RACINE DU SERVEUR
  //
  // Exemple :
  // environment.apiUrl = https://localhost:7263/api
  //
  // => serverUrl = https://localhost:7263
  // ==========================================================

  private readonly serverUrl =
    environment.apiUrl.replace(/\/api\/?$/, '');


  // ==========================================================
  // RÉFÉRENTIEL : TYPES DE BIENS
  // ==========================================================

  readonly typesBien = [
    {
      label: 'Immeuble',
      value: TypeBien.Immeuble
    },
    {
      label: 'Maison',
      value: TypeBien.Maison
    },
    {
      label: 'Villa',
      value: TypeBien.Villa
    },
    {
      label: 'Boutique',
      value: TypeBien.Boutique
    },
    {
      label: 'Terrain',
      value: TypeBien.Terrain
    },
    {
      label: 'Bureau',
      value: TypeBien.Bureau
    },
    {
      label: 'Entrepôt',
      value: TypeBien.Entrepot
    },
    {
      label: 'Parking',
      value: TypeBien.Parking
    },
    {
      label: 'Magasin',
      value: TypeBien.Magasin
    },
    {
      label: 'Autre',
      value: TypeBien.Autre
    }
  ];


  // ==========================================================
  // URL D'UNE PHOTO
  // ==========================================================

  getPhotoUrl(
    photo: string | null | undefined
  ): string {

    if (!photo) {
      return '';
    }

    // Si le backend renvoie déjà une URL absolue
    if (
      photo.startsWith('http://') ||
      photo.startsWith('https://')
    ) {
      return photo;
    }

    return `${this.serverUrl}${
      photo.startsWith('/') ? '' : '/'
    }${photo}`;
  }


  // ==========================================================
  // LIBELLÉ TYPE
  // ==========================================================

  getTypeLibelle(
    type: TypeBien
  ): string {

    const found = this.typesBien.find(
      t => t.value === type
    );

    return found?.label ?? 'Inconnu';
  }


  // ==========================================================
  // GET : LISTE DES BIENS
  //
  // GET /api/BienImmobilier
  //
  // Le backend filtre déjà par SocieteId.
  // ==========================================================

  getBiens(): Observable<BienDto[]> {

    return this.http.get<BienDto[]>(
      this.apiUrl
    );
  }


  // ==========================================================
  // GET : DÉTAIL D'UN BIEN
  //
  // GET /api/BienImmobilier/{id}
  // ==========================================================

  getBienById(
    id: string
  ): Observable<BienDetailDto> {

    return this.http.get<BienDetailDto>(
      `${this.apiUrl}/${id}`
    );
  }


  
private ajouterChamp(
  formData: FormData,
  nom: string,
  valeur: string | number | null | undefined
): void {
  if (valeur !== null && valeur !== undefined && valeur !== '') {
    formData.append(nom, String(valeur));
  }
}

private construireFormData(
  request: CreerBienRequest | ModifierBienRequest,
  photosExistantes?: string[]
): FormData {
  const formData = new FormData();

  this.ajouterChamp(formData, 'Reference', request.reference);
  this.ajouterChamp(formData, 'Nom', request.nom);
  this.ajouterChamp(formData, 'Type', request.type);
  this.ajouterChamp(formData, 'Adresse', request.adresse);
  this.ajouterChamp(formData, 'Ville', request.ville);
  this.ajouterChamp(formData, 'Quartier', request.quartier);
  this.ajouterChamp(formData, 'Region', request.region);
  this.ajouterChamp(formData, 'Departement', request.departement);
  this.ajouterChamp(formData, 'Arrondissement', request.arrondissement);

  this.ajouterChamp(formData, 'Superficie', request.superficie);
  this.ajouterChamp(formData, 'AnneeConstruction', request.anneeConstruction);
  this.ajouterChamp(formData, 'NombreEtages', request.nombreEtages);

  this.ajouterChamp(
    formData, 'NumeroTitreFoncier', request.numeroTitreFoncier
  );
  this.ajouterChamp(formData, 'NumeroLot', request.numeroLot);
  this.ajouterChamp(formData, 'NumeroParcelle', request.numeroParcelle);
  this.ajouterChamp(
    formData, 'ReferenceCadastrale', request.referenceCadastrale
  );

  this.ajouterChamp(
    formData, 'ValeurAcquisition', request.valeurAcquisition
  );
  this.ajouterChamp(formData, 'DateAcquisition', request.dateAcquisition);
  this.ajouterChamp(formData, 'ValeurEstimee', request.valeurEstimee);
  this.ajouterChamp(
    formData, 'DateDerniereEvaluation', request.dateDerniereEvaluation
  );
  this.ajouterChamp(formData, 'StatutFoncier', request.statutFoncier);

  photosExistantes?.forEach(photo => {
    formData.append('PhotosExistantes', photo);
  });

  request.fichiers?.forEach(fichier => {
    formData.append('Fichiers', fichier, fichier.name);
  });

  return formData;
}

createBien(request: CreerBienRequest): Observable<BienDto> {
  const formData = this.construireFormData(request);

  return this.http.post<BienDto>(this.apiUrl, formData);
}

updateBien(
  id: string,
  request: ModifierBienRequest
): Observable<unknown> {
  const formData = this.construireFormData(
    request,
    request.photosExistantes
  );

  return this.http.put<unknown>(`${this.apiUrl}/${id}`, formData);
}


  // ==========================================================
  // DELETE : SUPPRESSION LOGIQUE
  //
  // DELETE /api/BienImmobilier/{id}
  // ==========================================================

  deleteBien(
    id: string
  ): Observable<SupprimerBienResponse> {

    return this.http.delete<SupprimerBienResponse>(
      `${this.apiUrl}/${id}`
    );
  }

  getStatutUniteLibelle(statut: number): string {
  switch (statut) {
    case 1:
      return 'Disponible';

    case 2:
      return 'Louée';

    case 3:
      return 'Réservée';

    case 4:
      return 'En maintenance';

    default:
      return 'Inconnu';
  }
}

}