import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment.development';

import {
  DeclarationChargeBienDto,
  AjouterDeclarationChargeBienRequest,
  ModifierDeclarationChargeBienRequest
} from '../models/gestimmo.models';

export interface DeclarationChargeBienResponse {
  id: string;
  declarationFiscaleId: string;
  chargeBienId: string;
  descriptionCharge: string;
  dateDepense: string;
  montantTTC: number;
  statutDeductibilite: number;
  montantRetenu: number;
  dateCreation?: string;
}

export interface ChargeBienDisponibleDto {
  id: string;
  description: string;
  dateDepense: string;
  montantTTC: number;
  statutDeductibilite: number;
  montantDeductibleFiscalement: number;
  bienImmobilierId: string;
  bienImmobilierNom: string;
  bienImmobilierReference?: string;
}

@Injectable({
  providedIn: 'root'
})
export class DeclarationChargeBiensService {

  private readonly apiUrl =
    `${environment.apiUrl}/DeclarationChargeBiens`;

  constructor(
    private readonly http: HttpClient
  ) {}

  // ============================================================
  // CONSULTER LES CHARGES DÉJÀ ASSOCIÉES À UNE DÉCLARATION
  // GET : api/DeclarationChargeBiens/declaration/{declarationId}
  // ============================================================

  getByDeclaration(
    declarationId: string
  ): Observable<DeclarationChargeBienDto[]> {

    return this.http.get<DeclarationChargeBienDto[]>(
      `${this.apiUrl}/declaration/${declarationId}`
    );
  }

  // ============================================================
  // CONSULTER LES CHARGES DISPONIBLES DE TOUS LES IMMEUBLES
  // GET : api/DeclarationChargeBiens/disponibles/{declarationId}
  // ============================================================

  getDisponibles(
    declarationId: string
  ): Observable<ChargeBienDisponibleDto[]> {

    return this.http.get<ChargeBienDisponibleDto[]>(
      `${this.apiUrl}/disponibles/${declarationId}`
    );
  }

  // ============================================================
  // AJOUTER UNE CHARGE À UNE DÉCLARATION
  // POST : api/DeclarationChargeBiens
  // ============================================================

  ajouter(
    request: AjouterDeclarationChargeBienRequest
  ): Observable<DeclarationChargeBienResponse> {

    return this.http.post<DeclarationChargeBienResponse>(
      this.apiUrl,
      request
    );
  }

  // ============================================================
  // MODIFIER LE MONTANT RETENU
  // PUT : api/DeclarationChargeBiens/{id}
  // ============================================================

  modifier(
    id: string,
    request: ModifierDeclarationChargeBienRequest
  ): Observable<void> {

    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      request
    );
  }

  // ============================================================
  // RETIRER UNE CHARGE D'UNE DÉCLARATION
  // DELETE : api/DeclarationChargeBiens/{id}
  // ============================================================

  retirer(
    id: string
  ): Observable<void> {

    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }
}