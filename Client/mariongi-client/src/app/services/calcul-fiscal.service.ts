import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CalculFiscalResponse,
  DeclarationFiscaleDto,
  DeclarationChargeBienDto,
  AjouterDeclarationChargeBienRequest,
  ModifierDeclarationChargeBienRequest
} from '../models/gestimmo.models';

import { DeclarationsFiscalesService } from './declarations-fiscales.service';
import { DeclarationChargeBienResponse, DeclarationChargeBiensService } from './declaration-charge-biens.service';

@Injectable({
  providedIn: 'root'
})
export class CalculFiscalService {

  constructor(
    private readonly declarationsService: DeclarationsFiscalesService,
    private readonly chargesService: DeclarationChargeBiensService
  ) {}

  // ============================================================
  // CONSULTER UNE DÉCLARATION
  // ============================================================

  getDeclaration(
    declarationId: string
  ): Observable<DeclarationFiscaleDto> {

    return this.declarationsService.getById(declarationId);
  }

  // ============================================================
  // CALCULER LA DÉCLARATION FISCALE
  // POST : api/DeclarationsFiscales/{id}/calculer
  // ============================================================

  calculer(
    declarationId: string
  ): Observable<CalculFiscalResponse> {

    return this.declarationsService.calculer(declarationId);
  }

  // ============================================================
  // CONSULTER LES CHARGES RETENUES
  // GET : api/DeclarationChargeBiens/declaration/{id}
  // ============================================================

  getChargesDeclaration(
    declarationId: string
  ): Observable<DeclarationChargeBienDto[]> {

    return this.chargesService.getByDeclaration(declarationId);
  }

  // ============================================================
  // AJOUTER UNE CHARGE À LA DÉCLARATION
  // ============================================================

ajouterCharge(
  request: AjouterDeclarationChargeBienRequest
): Observable<DeclarationChargeBienResponse> {

  return this.chargesService.ajouter(request);
}

  // ============================================================
  // MODIFIER LE MONTANT RETENU D'UNE CHARGE
  // ============================================================

  modifierCharge(
    id: string,
    request: ModifierDeclarationChargeBienRequest
  ): Observable<void> {

    return this.chargesService.modifier(id, request);
  }

  // ============================================================
  // RETIRER UNE CHARGE DE LA DÉCLARATION
  // ============================================================

  retirerCharge(
    id: string
  ): Observable<void> {

    return this.chargesService.retirer(id);
  }
}
