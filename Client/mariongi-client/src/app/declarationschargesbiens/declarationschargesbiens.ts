import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';

import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TableModule } from 'primeng/table';
import { ToastModule } from 'primeng/toast';

import {
  AjouterDeclarationChargeBienRequest,
  ChargeBienDto,
  DeclarationChargeBienDto,
  DeclarationFiscaleDto,
  ModifierDeclarationChargeBienRequest,
  StatutDeductibilite
} from '../models/gestimmo.models';

import { ChargesBiensService } from '../services/charges-biens.service';
import { DeclarationChargeBiensService } from '../services/declaration-charge-biens.service';
import { DeclarationsFiscalesService } from '../services/declarations-fiscales.service';

type ModeDialogue = 'ajout' | 'modification';

@Component({
  selector: 'app-declarationschargesbiens',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DatePipe,
    ButtonModule,
    TableModule,
    ToastModule,
    ConfirmDialogModule
  ],
  templateUrl: './declarationschargesbiens.html',
  styleUrl: './declarationschargesbiens.scss'
})
export class DeclarationsChargesBiens implements OnInit {

  private readonly declarationsService = inject(DeclarationsFiscalesService);
  private readonly associationsService = inject(DeclarationChargeBiensService);
  private readonly chargesService = inject(ChargesBiensService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  declarations: DeclarationFiscaleDto[] = [];
  chargesAssociees: DeclarationChargeBienDto[] = [];
  toutesLesCharges: ChargeBienDto[] = [];

  declarationSelectionneeId = '';

  chargementDeclarations = false;
  chargementDonnees = false;
  traitementEnCours = false;

  dialogueVisible = false;
  modeDialogue: ModeDialogue = 'ajout';

  chargeSelectionneeId = '';
  associationSelectionnee: DeclarationChargeBienDto | null = null;
  montantRetenuSaisi: number | null = null;

  ngOnInit(): void {
    this.chargerDeclarations();
  }

  // ============================================================
  // Chargement des déclarations
  // ============================================================

  chargerDeclarations(): void {
    this.chargementDeclarations = true;

    this.declarationsService.getAll().subscribe({
      next: declarations => {
        this.declarations = declarations ?? [];
        this.chargementDeclarations = false;

        if (this.declarations.length > 0) {
          this.declarationSelectionneeId = this.declarations[0].id;
          this.chargerDonneesDeclaration();
        }
      },
      error: erreur => {
        this.chargementDeclarations = false;

        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: this.getMessageErreur(
            erreur,
            'Impossible de charger les déclarations fiscales.'
          )
        });
      }
    });
  }

  // ============================================================
  // Sélection d'une déclaration
  // ============================================================

  changerDeclaration(): void {
    if (!this.declarationSelectionneeId) {
      this.chargesAssociees = [];
      this.toutesLesCharges = [];
      return;
    }

    this.chargerDonneesDeclaration();
  }

  get declarationSelectionnee(): DeclarationFiscaleDto | undefined {
    return this.declarations.find(
      declaration => declaration.id === this.declarationSelectionneeId
    );
  }

  // ============================================================
  // Chargement des charges et associations
  // ============================================================

  chargerDonneesDeclaration(): void {
    const declaration = this.declarationSelectionnee;

    if (!declaration) {
      return;
    }

    this.chargementDonnees = true;

    const debutAnnee = `${declaration.anneeFiscale}-01-01`;
    const finAnnee = `${declaration.anneeFiscale}-12-31`;

    const filtres: {
      dateDebut: string;
      dateFin: string;
      bienImmobilierId?: string;
    } = {
      dateDebut: debutAnnee,
      dateFin: finAnnee
    };

    if (declaration.bienImmobilierId) {
      filtres.bienImmobilierId = declaration.bienImmobilierId;
    }

    let associationsChargees = false;
    let chargesChargees = false;

    const terminerChargement = (): void => {
      if (associationsChargees && chargesChargees) {
        this.chargementDonnees = false;
      }
    };

    this.associationsService
      .getByDeclaration(declaration.id)
      .subscribe({
        next: associations => {
          this.chargesAssociees = associations ?? [];
          associationsChargees = true;
          terminerChargement();
        },
        error: erreur => {
          associationsChargees = true;
          terminerChargement();

          this.messageService.add({
            severity: 'error',
            summary: 'Erreur',
            detail: this.getMessageErreur(
              erreur,
              'Impossible de charger les charges associées à cette déclaration.'
            )
          });
        }
      });

    this.chargesService.getCharges(filtres).subscribe({
      next: charges => {
        this.toutesLesCharges = charges ?? [];
        chargesChargees = true;
        terminerChargement();
      },
      error: erreur => {
        chargesChargees = true;
        terminerChargement();

        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: this.getMessageErreur(
            erreur,
            'Impossible de charger les charges immobilières.'
          )
        });
      }
    });
  }

  // ============================================================
  // Charges admissibles non encore associées
  // ============================================================

  get chargesDisponibles(): ChargeBienDto[] {
    const idsAssocies = new Set(
      this.chargesAssociees.map(charge => charge.chargeBienId)
    );

    return this.toutesLesCharges.filter(charge => {
      const statutAdmissible =
        charge.statutDeductibilite === StatutDeductibilite.Deductible ||
        charge.statutDeductibilite === StatutDeductibilite.PartiellementDeductible;

      const montantAdmissible =
        (charge.montantDeductibleFiscalement ?? 0) > 0;

      return (
        statutAdmissible &&
        montantAdmissible &&
        !idsAssocies.has(charge.id)
      );
    });
  }

  // ============================================================
  // Dialogue d'ajout
  // ============================================================

  ouvrirDialogueAjout(): void {
    if (!this.declarationSelectionnee) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Sélection requise',
        detail: 'Veuillez sélectionner une déclaration fiscale.'
      });
      return;
    }

    if (this.chargesDisponibles.length === 0) {
      this.messageService.add({
        severity: 'info',
        summary: 'Aucune charge disponible',
        detail: 'Aucune charge admissible supplémentaire ne peut être associée à cette déclaration.'
      });
      return;
    }

    this.modeDialogue = 'ajout';
    this.associationSelectionnee = null;

    this.chargeSelectionneeId = this.chargesDisponibles[0].id;

    this.montantRetenuSaisi =
      this.chargesDisponibles[0].montantDeductibleFiscalement ?? 0;

    this.dialogueVisible = true;
  }

  changerChargeSelectionnee(): void {
    const charge = this.chargesDisponibles.find(
      item => item.id === this.chargeSelectionneeId
    );

    this.montantRetenuSaisi =
      charge?.montantDeductibleFiscalement ?? null;
  }

  get chargeCandidate(): ChargeBienDto | undefined {
    return this.chargesDisponibles.find(
      charge => charge.id === this.chargeSelectionneeId
    );
  }

  // ============================================================
  // Dialogue de modification
  // ============================================================

  ouvrirDialogueModification(
    association: DeclarationChargeBienDto
  ): void {
    this.modeDialogue = 'modification';
    this.associationSelectionnee = association;

    this.chargeSelectionneeId = association.chargeBienId;
    this.montantRetenuSaisi = association.montantRetenu;

    this.dialogueVisible = true;
  }

  // ============================================================
  // Enregistrement
  // ============================================================

  enregistrer(): void {
    if (
      !this.declarationSelectionneeId ||
      this.montantRetenuSaisi === null ||
      !Number.isFinite(this.montantRetenuSaisi) ||
      this.montantRetenuSaisi <= 0
    ) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Données invalides',
        detail: 'Veuillez saisir un montant retenu supérieur à zéro.'
      });
      return;
    }

    if (this.modeDialogue === 'ajout') {
      this.ajouterCharge();
      return;
    }

    this.modifierCharge();
  }

  private ajouterCharge(): void {
    const charge = this.chargeCandidate;

    if (!charge) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Charge invalide',
        detail: 'Veuillez sélectionner une charge admissible.'
      });
      return;
    }

    const montantMaximum = charge.montantDeductibleFiscalement ?? 0;

    if (this.montantRetenuSaisi! > montantMaximum) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Montant invalide',
        detail: `Le montant retenu ne peut pas dépasser ${this.formaterMontant(montantMaximum)}.`
      });
      return;
    }

    const request: AjouterDeclarationChargeBienRequest = {
      declarationFiscaleId: this.declarationSelectionneeId,
      chargeBienId: charge.id,
      montantRetenu: this.montantRetenuSaisi!
    };

    this.traitementEnCours = true;

    this.associationsService.ajouter(request).subscribe({
      next: () => {
        this.traitementEnCours = false;
        this.dialogueVisible = false;

        this.messageService.add({
          severity: 'success',
          summary: 'Association créée',
          detail: 'La charge a été associée à la déclaration fiscale.'
        });

        this.chargerDonneesDeclaration();
      },
      error: erreur => {
        this.traitementEnCours = false;

        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: this.getMessageErreur(
            erreur,
            'Impossible d’associer cette charge à la déclaration.'
          )
        });
      }
    });
  }

  private modifierCharge(): void {
    if (!this.associationSelectionnee) {
      return;
    }

    const montantMaximum = this.associationSelectionnee.montantTTC;

    if (this.montantRetenuSaisi! > montantMaximum) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Montant invalide',
        detail: `Le montant retenu ne peut pas dépasser le montant TTC de la charge (${this.formaterMontant(montantMaximum)}).`
      });
      return;
    }

    const request: ModifierDeclarationChargeBienRequest = {
      montantRetenu: this.montantRetenuSaisi!
    };

    this.traitementEnCours = true;

    this.associationsService
      .modifier(this.associationSelectionnee.id, request)
      .subscribe({
        next: () => {
          this.traitementEnCours = false;
          this.dialogueVisible = false;

          this.messageService.add({
            severity: 'success',
            summary: 'Modification enregistrée',
            detail: 'Le montant retenu a été mis à jour.'
          });

          this.chargerDonneesDeclaration();
        },
        error: erreur => {
          this.traitementEnCours = false;

          this.messageService.add({
            severity: 'error',
            summary: 'Erreur',
            detail: this.getMessageErreur(
              erreur,
              'Impossible de modifier le montant retenu.'
            )
          });
        }
      });
  }

  // ============================================================
  // Suppression d'une association
  // ============================================================

  confirmerRetrait(association: DeclarationChargeBienDto): void {
    this.confirmationService.confirm({
      header: 'Retirer cette charge',
      message: `Voulez-vous retirer la charge « ${association.descriptionCharge} » de cette déclaration fiscale ?`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Oui, retirer',
      rejectLabel: 'Annuler',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.retirerCharge(association)
    });
  }

  private retirerCharge(
    association: DeclarationChargeBienDto
  ): void {
    this.traitementEnCours = true;

    this.associationsService.retirer(association.id).subscribe({
      next: () => {
        this.traitementEnCours = false;

        this.messageService.add({
          severity: 'success',
          summary: 'Charge retirée',
          detail: 'La charge a été retirée de la déclaration fiscale.'
        });

        this.chargerDonneesDeclaration();
      },
      error: erreur => {
        this.traitementEnCours = false;

        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: this.getMessageErreur(
            erreur,
            'Impossible de retirer cette charge.'
          )
        });
      }
    });
  }

  // ============================================================
  // Informations et calculs d'affichage
  // ============================================================

  get nombreChargesAssociees(): number {
    return this.chargesAssociees.length;
  }

  get totalMontantsRetenus(): number {
    return this.chargesAssociees.reduce(
      (total, charge) => total + (charge.montantRetenu ?? 0),
      0
    );
  }

  get totalMontantsTtc(): number {
    return this.chargesAssociees.reduce(
      (total, charge) => total + (charge.montantTTC ?? 0),
      0
    );
  }

  getStatutLibelle(statut: StatutDeductibilite): string {
    if (statut === StatutDeductibilite.Deductible) {
      return 'Déductible';
    }

    if (statut === StatutDeductibilite.PartiellementDeductible) {
      return 'Partiellement déductible';
    }

    return 'Non admissible';
  }

  getStatutClasse(statut: StatutDeductibilite): string {
    if (statut === StatutDeductibilite.Deductible) {
      return 'status-success';
    }

    if (statut === StatutDeductibilite.PartiellementDeductible) {
      return 'status-warning';
    }

    return 'status-danger';
  }

  formaterMontant(montant: number | null | undefined): string {
    return `${(montant ?? 0).toLocaleString('fr-FR', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 2
    })} FCFA`;
  }

  fermerDialogue(): void {
    if (this.traitementEnCours) {
      return;
    }

    this.dialogueVisible = false;
  }

  private getMessageErreur(
    erreur: unknown,
    messageParDefaut: string
  ): string {
    if (erreur instanceof HttpErrorResponse) {
      const detail = erreur.error?.message
        ?? erreur.error?.detail
        ?? erreur.error?.title;

      if (typeof detail === 'string' && detail.trim()) {
        return detail;
      }
    }

    return messageParDefaut;
  }
}