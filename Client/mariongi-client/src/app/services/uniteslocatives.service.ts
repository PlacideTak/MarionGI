import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  TypeUniteLocative,
  StatutBien,
  UniteLocativeDto,
  UniteLocativeDetailDto,
  CreerUniteLocativeRequest,
  ModifierUniteLocativeRequest,
  ModifierStatutUniteResponse,
  SupprimerUniteResponse
} from '../models/gestimmo.models';

import { environment } from '../../environments/environment.development';


@Injectable({
  providedIn: 'root'
})
export class UnitesLocativesService {

  private readonly http = inject(HttpClient);

  // ==========================================================
  // URL API
  // ==========================================================

  private readonly apiUrl =
    `${environment.apiUrl}/UnitesLocatives`;


  // ==========================================================
  // URL RACINE DU SERVEUR
  // ==========================================================

  private readonly serverUrl =
    environment.apiUrl.replace(/\/api\/?$/, '');


  // ==========================================================
  // RÉFÉRENTIEL : TYPES D'UNITÉS LOCATIVES
  // ==========================================================

  readonly typesUniteLocative = [
    {
      label: 'Appartement',
      value: TypeUniteLocative.Appartement
    },
    {
      label: 'Maison',
      value: TypeUniteLocative.Maison
    },
    {
      label: 'Villa',
      value: TypeUniteLocative.Villa
    },
    {
      label: 'Boutique',
      value: TypeUniteLocative.Boutique
    },
    {
      label: 'Terrain',
      value: TypeUniteLocative.Terrain
    },
    {
      label: 'Chambre',
      value: TypeUniteLocative.Chambre
    },
    {
      label: 'Studio',
      value: TypeUniteLocative.Studio
    },
    {
      label: 'Magasin',
      value: TypeUniteLocative.Magasin
    },
    {
      label: 'Bureau',
      value: TypeUniteLocative.Bureau
    },
    {
      label: 'Duplex',
      value: TypeUniteLocative.Duplex
    },
    {
      label: 'Bungalow',
      value: TypeUniteLocative.Bungalow
    },
    {
      label: 'Penthouse',
      value: TypeUniteLocative.Penthouse
    },
    {
      label: 'Entrepôt',
      value: TypeUniteLocative.Entrepot
    },
    {
      label: 'Parking',
      value: TypeUniteLocative.Parking
    },
    {
      label: 'Cave',
      value: TypeUniteLocative.Cave
    }
  ];


  // ==========================================================
  // RÉFÉRENTIEL : STATUTS
  // ==========================================================

  readonly statutsUnite = [
    {
      label: 'Disponible',
      value: StatutBien.Disponible
    },
    {
      label: 'Loué',
      value: StatutBien.Loue
    },
    {
      label: 'Réservé',
      value: StatutBien.Reserve
    },
    {
      label: 'En travaux',
      value: StatutBien.EnTravaux
    }
  ];


  // ==========================================================
  // URL PHOTO
  // ==========================================================

  getPhotoUrl(
    photo: string | null | undefined
  ): string {

    if (!photo) {
      return '';
    }

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
    type: TypeUniteLocative
  ): string {

    const found = this.typesUniteLocative.find(
      t => t.value === type
    );

    return found?.label ?? 'Inconnu';
  }


  // ==========================================================
  // LIBELLÉ STATUT
  // ==========================================================

  getStatutLibelle(
    statut: StatutBien
  ): string {

    const found = this.statutsUnite.find(
      s => s.value === statut
    );

    return found?.label ?? 'Inconnu';
  }


  // ==========================================================
  // SEVERITY STATUT
  // ==========================================================

  getStatutSeverity(
    statut: StatutBien
  ): 'success' | 'info' | 'warn' | 'danger' {

    switch (statut) {

      case StatutBien.Disponible:
        return 'success';

      case StatutBien.Loue:
        return 'info';

      case StatutBien.Reserve:
        return 'warn';

      case StatutBien.EnTravaux:
        return 'danger';

      default:
        return 'danger';
    }
  }


  // ==========================================================
  // GET : TOUTES LES UNITÉS
  // ==========================================================

  getUnites(): Observable<UniteLocativeDto[]> {

    return this.http.get<UniteLocativeDto[]>(
      this.apiUrl
    );
  }


  // ==========================================================
  // GET : DÉTAIL
  // ==========================================================

  getUniteById(
    id: string
  ): Observable<UniteLocativeDetailDto> {

    return this.http.get<UniteLocativeDetailDto>(
      `${this.apiUrl}/${id}`
    );
  }


  // ==========================================================
  // GET : UNITÉS D'UN BIEN
  // ==========================================================

  getUnitesParBien(
    bienImmobilierId: string
  ): Observable<UniteLocativeDto[]> {

    return this.http.get<UniteLocativeDto[]>(
      `${this.apiUrl}/bien/${bienImmobilierId}`
    );
  }


  // ==========================================================
  // POST : CRÉER
  // ==========================================================

  createUnite(
    request: CreerUniteLocativeRequest
  ): Observable<UniteLocativeDto> {

    const formData = this.creerFormData(request);

    return this.http.post<UniteLocativeDto>(
      this.apiUrl,
      formData
    );
  }


  // ==========================================================
  // PUT : MODIFIER
  // ==========================================================

  updateUnite(
    id: string,
    request: ModifierUniteLocativeRequest
  ): Observable<void> {

    const formData = this.creerFormData(request);

    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      formData
    );
  }


  // ==========================================================
  // CONSTRUCTION DU FORMDATA
  // ==========================================================


private creerFormData(
  request:
    CreerUniteLocativeRequest |
    ModifierUniteLocativeRequest
): FormData {

  const formData = new FormData();

  // --------------------------------------------------------
  // CHAMPS OBLIGATOIRES
  // --------------------------------------------------------

  formData.append('Reference', request.reference);
  formData.append('Type', String(request.type));
  formData.append('Superficie', String(request.superficie));
  formData.append('Loyer', String(request.loyer));
  formData.append('Statut', String(request.statut));
  formData.append('BienImmobilierId', request.bienImmobilierId);

  // --------------------------------------------------------
  // CHAMPS OPTIONNELS
  // --------------------------------------------------------

  const champs = request as CreerUniteLocativeRequest & {
    nombrePieces?: number | null;
    etage?: number | null;
    chargesLocativesMensuelles?: number | null;
    montantCaution?: number | null;
    datePremiereMiseEnLocation?: string | Date | null;
    dateDerniereRenovation?: string | Date | null;
    estExonereeFiscalement?: boolean;
    motifExoneration?: string | null;
  };

  if (champs.nombrePieces != null) {
    formData.append('NombrePieces', String(champs.nombrePieces));
  }

  if (champs.etage != null) {
    formData.append('Etage', String(champs.etage));
  }

  if (champs.chargesLocativesMensuelles != null) {
    formData.append(
      'ChargesLocativesMensuelles',
      String(champs.chargesLocativesMensuelles)
    );
  }

  if (champs.montantCaution != null) {
    formData.append(
      'MontantCaution',
      String(champs.montantCaution)
    );
  }

 if (champs.datePremiereMiseEnLocation) {
  formData.append(
    'DatePremiereMiseEnLocation',
    String(champs.datePremiereMiseEnLocation).substring(0, 10)
  );
}

if (champs.dateDerniereRenovation) {
  formData.append(
    'DateDerniereRenovation',
    String(champs.dateDerniereRenovation).substring(0, 10)
  );
}

  formData.append(
    'EstExonereeFiscalement',
    String(champs.estExonereeFiscalement ?? false)
  );

  if (champs.motifExoneration?.trim()) {
    formData.append(
      'MotifExoneration',
      champs.motifExoneration.trim()
    );
  }

  // --------------------------------------------------------
  // PHOTOS EXISTANTES
  // --------------------------------------------------------

  if (
    'photosExistantes' in request &&
    request.photosExistantes
  ) {
    for (const photo of request.photosExistantes) {
      if (photo?.trim()) {
        formData.append('PhotosExistantes', photo);
      }
    }
  }

  // --------------------------------------------------------
  // NOUVELLES PHOTOS
  // --------------------------------------------------------

  if (request.fichiers?.length) {
    for (const fichier of request.fichiers) {
      if (fichier) {
        formData.append('Fichiers', fichier, fichier.name);
      }
    }
  }

  return formData;
}



  // ==========================================================
  // PATCH : MODIFIER LE STATUT
  // ==========================================================

  updateStatut(
    id: string,
    statut: StatutBien
  ): Observable<ModifierStatutUniteResponse> {

    return this.http.patch<ModifierStatutUniteResponse>(
      `${this.apiUrl}/${id}/statut`,
      {
        statut
      }
    );
  }


  // ==========================================================
  // DELETE
  // ==========================================================

  deleteUnite(
    id: string
  ): Observable<SupprimerUniteResponse> {

    return this.http.delete<SupprimerUniteResponse>(
      `${this.apiUrl}/${id}`
    );
  }
}