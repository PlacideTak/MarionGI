
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Subscription } from 'rxjs';

import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';

import {
  CalculFiscalResponse,
  DeclarationFiscaleDto,
  DeclarationChargeBienDto,
  ModeDeductionCharges,
  StatutDeductibilite,
  TypeImpotFiscal
} from '../models/gestimmo.models';

import { CalculFiscalService } from '../services/calcul-fiscal.service';
import { DeclarationsFiscalesService } from '../services/declarations-fiscales.service';

import {
  DeclarationChargeBiensService,
  ChargeBienDisponibleDto
} from '../services/declaration-charge-biens.service';

interface ChargeDeclarationAffichee extends DeclarationChargeBienDto {
  montantRetenuModifie: number;
  modificationEnCours: boolean;
}

interface ChargeDisponibleAffichee extends ChargeBienDisponibleDto {
  montantRetenuModifie: number;
}

@Component({
  selector: 'app-calculfiscal',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CurrencyPipe,
    DatePipe,
    TableModule,
    ButtonModule,
    ConfirmDialogModule,
    InputNumberModule,
    TagModule,
    ToastModule,
    TooltipModule
  ],
  providers: [
    MessageService,
    ConfirmationService
  ],
  templateUrl: './calculfiscal.html',
  styleUrl: './calculfiscal.scss'
})
export class Calculfiscal implements OnInit, OnDestroy {

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private readonly calculFiscalService = inject(CalculFiscalService);
  private readonly declarationsService = inject(DeclarationsFiscalesService);
  private readonly declarationChargesService = inject(DeclarationChargeBiensService);

  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  private routeSubscription?: Subscription;

  declarationId = '';

  declarationsDisponibles: DeclarationFiscaleDto[] = [];

  declaration: DeclarationFiscaleDto | null = null;
  resultat: CalculFiscalResponse | null = null;

  charges: ChargeDeclarationAffichee[] = [];
  chargesDisponibles: ChargeDisponibleAffichee[] = [];

  chargement = false;
  chargementListe = false;
  chargementCharges = false;
  chargementChargesDisponibles = false;
  enCoursCalcul = false;

  ajoutEnCoursId: string | null = null;

  readonly ModeDeductionCharges = ModeDeductionCharges;
  readonly StatutDeductibilite = StatutDeductibilite;
  readonly TypeImpotFiscal = TypeImpotFiscal;

  // ============================================================
  // INITIALISATION
  // ============================================================

  ngOnInit(): void {

    this.routeSubscription = this.route.paramMap.subscribe(params => {

      const nouvelId = params.get('id') ?? '';

      this.declarationId = nouvelId;

      this.declaration = null;
      this.resultat = null;

      this.charges = [];
      this.chargesDisponibles = [];

      this.chargement = false;
      this.chargementCharges = false;
      this.chargementChargesDisponibles = false;

      this.ajoutEnCoursId = null;
      this.enCoursCalcul = false;

      if (this.declarationId) {
        this.chargerDeclaration();
      } else {
        this.chargerDeclarationsDisponibles();
      }
    });
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
  }

  // ============================================================
  // LISTE DES DÉCLARATIONS
  // ============================================================

  chargerDeclarationsDisponibles(): void {

    this.chargementListe = true;

    this.declarationsService.getAll().subscribe({

      next: declarations => {

        this.declarationsDisponibles = declarations
          .slice()
          .sort((a, b) => {

            if (b.anneeFiscale !== a.anneeFiscale) {
              return b.anneeFiscale - a.anneeFiscale;
            }

            return a.typeImpot - b.typeImpot;
          });

        this.chargementListe = false;
      },

      error: (erreur: HttpErrorResponse) => {

        this.chargementListe = false;

        this.messageService.add({
          severity: 'error',
          summary: 'Erreur de chargement',
          detail: this.obtenirMessageErreur(
            erreur,
            'Impossible de charger les déclarations fiscales.'
          ),
          life: 5000
        });
      }
    });
  }

  ouvrirDeclaration(declaration: DeclarationFiscaleDto): void {

    if (!declaration?.id) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Déclaration invalide',
        detail: 'L’identifiant de la déclaration est introuvable.'
      });

      return;
    }

    this.router.navigate(['/calculfiscal', declaration.id]);
  }

  revenirAuxDeclarations(): void {
    this.router.navigate(['/calculfiscal']);
  }

  // ============================================================
  // TYPE D'IMPÔT ET PÉRIMÈTRE FISCAL
  // ============================================================

  getTypeImpotLibelle(type: TypeImpotFiscal | number): string {

    switch (type) {

      case TypeImpotFiscal.ProprieteFonciere:
        return 'Propriété foncière';

      case TypeImpotFiscal.RevenusFonciers:
        return 'Revenus fonciers';

      case TypeImpotFiscal.PrecompteSurLoyers:
        return 'Précompte sur loyers';

      case TypeImpotFiscal.IRPP:
        return 'IRPP';

      case TypeImpotFiscal.ImpotSocietes:
        return 'Impôt sur les sociétés';

      case TypeImpotFiscal.PlusValueImmobiliere:
        return 'Plus-value immobilière';

      case TypeImpotFiscal.DroitsEnregistrement:
        return 'Droits d’enregistrement';

      case TypeImpotFiscal.TVA:
        return 'TVA';

      case TypeImpotFiscal.Autre:
        return 'Autre impôt';

      default:
        return 'Non défini';
    }
  }

  getPerimetreDeclaration(declaration: DeclarationFiscaleDto): string {

    if (
      declaration.typeImpot === TypeImpotFiscal.RevenusFonciers &&
      declaration.bienImmobilierId === null
    ) {
      return 'Patrimoine immobilier global';
    }

    if (
      declaration.typeImpot === TypeImpotFiscal.ProprieteFonciere &&
      declaration.bienImmobilierId
    ) {
      return declaration.bienImmobilier?.nom ?? 'Immeuble spécifique';
    }

    if (declaration.bienImmobilierId) {
      return declaration.bienImmobilier?.nom ?? 'Immeuble spécifique';
    }

    return 'Société';
  }

  getPerimetreDescription(): string {

    if (!this.declaration) {
      return '';
    }

    if (this.declarationGlobaleSociete) {

      return 'Cette déclaration couvre les revenus fonciers de tous les immeubles appartenant à la société pour cette année fiscale. Les loyers et les charges sont donc pris en compte à l’échelle de l’ensemble du patrimoine immobilier.';
    }

    if (
      this.declaration.typeImpot === TypeImpotFiscal.RevenusFonciers &&
      !this.declarationGlobaleSociete
    ) {

      return 'Attention : une déclaration de revenus fonciers doit être globale à la société et ne doit pas être rattachée à un immeuble particulier.';
    }

    if (this.declarationParImmeuble) {

      return 'Cette déclaration concerne uniquement l’immeuble indiqué.';
    }

    return 'Cette déclaration est rattachée à la société et à son année fiscale.';
  }

  get declarationGlobaleSociete(): boolean {

    return this.declaration !== null &&
      this.declaration.typeImpot === TypeImpotFiscal.RevenusFonciers &&
      this.declaration.bienImmobilierId === null;
  }

  get declarationParImmeuble(): boolean {
    return !!this.declaration?.bienImmobilierId;
  }

  get estDeclarationRevenusFonciers(): boolean {

    return this.declaration?.typeImpot ===
      TypeImpotFiscal.RevenusFonciers;
  }

  get peutCalculer(): boolean {

    return this.declarationGlobaleSociete &&
      !this.chargement &&
      !this.chargementCharges &&
      !this.chargementChargesDisponibles &&
      !this.enCoursCalcul &&
      !this.ajoutEnCoursId;
  }

  get afficherResultats(): boolean {
    return this.declarationGlobaleSociete;
  }

  get modeFraisReels(): boolean {

    return this.modeDeduction ===
      ModeDeductionCharges.FraisReels;
  }

  // ============================================================
  // CHARGEMENT DE LA DÉCLARATION
  // ============================================================

  /**
   * conserverResultat = true permet de recharger la déclaration
   * sans effacer la réponse du dernier calcul fiscal.
   */
  chargerDeclaration(conserverResultat = false): void {

    if (!this.declarationId) {
      return;
    }

    this.chargement = true;

    this.calculFiscalService
      .getDeclaration(this.declarationId)
      .subscribe({

        next: declaration => {

          this.declaration = declaration;

          if (!conserverResultat) {
            this.resultat = null;
          }

          this.chargement = false;

          // Les charges sont chargées uniquement
          // lorsque le mode Frais réels est sélectionné.

          if (
            this.declarationGlobaleSociete &&
            this.modeFraisReels
          ) {

            this.chargerCharges();
            this.chargerChargesDisponibles();

          } else {

            this.charges = [];
            this.chargesDisponibles = [];

            this.chargementCharges = false;
            this.chargementChargesDisponibles = false;
          }
        },

        error: (erreur: HttpErrorResponse) => {

          this.chargement = false;

          this.messageService.add({
            severity: 'error',
            summary: 'Erreur de chargement',
            detail: this.obtenirMessageErreur(
              erreur,
              'Impossible de charger la déclaration fiscale.'
            ),
            life: 5000
          });
        }
      });
  }

  // ============================================================
  // CHARGEMENT DES CHARGES DÉJÀ ASSOCIÉES
  // ============================================================

  chargerCharges(): void {

    if (!this.declarationId) {
      return;
    }

    this.chargementCharges = true;

    this.calculFiscalService
      .getChargesDeclaration(this.declarationId)
      .subscribe({

        next: charges => {

          this.charges = charges.map(charge => ({
            ...charge,
            montantRetenuModifie: charge.montantRetenu,
            modificationEnCours: false
          }));

          this.chargementCharges = false;
        },

        error: (erreur: HttpErrorResponse) => {

          this.chargementCharges = false;

          this.messageService.add({
            severity: 'error',
            summary: 'Erreur de chargement',
            detail: this.obtenirMessageErreur(
              erreur,
              'Impossible de charger les charges de la déclaration.'
            ),
            life: 5000
          });
        }
      });
  }

  // ============================================================
  // CHARGES DISPONIBLES POUR LA DÉCLARATION GLOBALE
  // ============================================================

  chargerChargesDisponibles(): void {

    if (!this.declarationId || !this.declarationGlobaleSociete) {

      this.chargesDisponibles = [];
      return;
    }

    if (!this.modeFraisReels) {

      this.chargesDisponibles = [];
      return;
    }

    this.chargementChargesDisponibles = true;

    this.declarationChargesService
      .getDisponibles(this.declarationId)
      .subscribe({

        next: charges => {

          this.chargesDisponibles = charges.map(charge => ({
            ...charge,
            montantRetenuModifie: charge.montantDeductibleFiscalement
          }));

          this.chargementChargesDisponibles = false;
        },

        error: (erreur: HttpErrorResponse) => {

          this.chargementChargesDisponibles = false;

          this.messageService.add({
            severity: 'error',
            summary: 'Erreur de chargement',
            detail: this.obtenirMessageErreur(
              erreur,
              'Impossible de charger les charges disponibles.'
            ),
            life: 5000
          });
        }
      });
  }

  // ============================================================
  // ASSOCIATION D'UNE CHARGE À LA DÉCLARATION
  // ============================================================

  ajouterCharge(charge: ChargeDisponibleAffichee): void {

    if (!this.declarationId || !this.declarationGlobaleSociete) {
      return;
    }

    if (!this.modeFraisReels) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Mode de déduction incompatible',
        detail: 'Les charges réelles ne peuvent être retenues que lorsque le mode Frais réels est sélectionné.'
      });

      return;
    }

    const montant = Number(charge.montantRetenuModifie);
    const montantMaximum = Number(charge.montantDeductibleFiscalement);

    if (!Number.isFinite(montant) || montant <= 0) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Montant invalide',
        detail: 'Le montant retenu doit être supérieur à zéro.'
      });

      return;
    }

    if (montant > montantMaximum) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Montant trop élevé',
        detail: 'Le montant retenu ne peut pas dépasser le montant fiscalement déductible de cette charge.'
      });

      return;
    }

    this.ajoutEnCoursId = charge.id;

    this.calculFiscalService
      .ajouterCharge({
        declarationFiscaleId: this.declarationId,
        chargeBienId: charge.id,
        montantRetenu: montant
      })
      .subscribe({

        next: () => {

          this.ajoutEnCoursId = null;
          this.resultat = null;

          this.messageService.add({
            severity: 'success',
            summary: 'Charge ajoutée',
            detail: `La charge « ${charge.description} » de l'immeuble « ${charge.bienImmobilierNom} » a été associée à la déclaration.`
          });

          this.chargerCharges();
          this.chargerChargesDisponibles();
        },

        error: (erreur: HttpErrorResponse) => {

          this.ajoutEnCoursId = null;

          this.messageService.add({
            severity: 'error',
            summary: 'Erreur d’association',
            detail: this.obtenirMessageErreur(
              erreur,
              'Impossible d’associer cette charge à la déclaration.'
            ),
            life: 5000
          });
        }
      });
  }

  // ============================================================
  // ACTUALISATION DE LA DÉCLARATION
  // ============================================================

  actualiserDeclaration(): void {

    if (!this.declarationId) {
      return;
    }

    this.chargerDeclaration();
  }

  // ============================================================
  // MODIFICATION DU MONTANT RETENU
  // ============================================================

  modifierMontantRetenu(charge: ChargeDeclarationAffichee): void {

    const montant = Number(charge.montantRetenuModifie);

    if (!Number.isFinite(montant) || montant <= 0) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Montant invalide',
        detail: 'Le montant retenu doit être supérieur à zéro.'
      });

      return;
    }

    if (montant > charge.montantTTC) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Montant invalide',
        detail: 'Le montant retenu ne peut pas dépasser le montant TTC de la charge.'
      });

      return;
    }

    if (this.modeDeduction !== ModeDeductionCharges.FraisReels) {
      return;
    }

    charge.modificationEnCours = true;

    this.calculFiscalService
      .modifierCharge(charge.id, {
        montantRetenu: montant
      })
      .subscribe({

        next: () => {

          charge.montantRetenu = montant;
          charge.montantRetenuModifie = montant;
          charge.modificationEnCours = false;

          this.resultat = null;

          this.messageService.add({
            severity: 'success',
            summary: 'Modification enregistrée',
            detail: 'Le montant retenu a été mis à jour. Relance le calcul fiscal pour actualiser les résultats.'
          });
        },

        error: (erreur: HttpErrorResponse) => {

          charge.modificationEnCours = false;

          this.messageService.add({
            severity: 'error',
            summary: 'Erreur de modification',
            detail: this.obtenirMessageErreur(
              erreur,
              'Impossible de modifier le montant retenu.'
            ),
            life: 5000
          });
        }
      });
  }

  // ============================================================
  // RETRAIT D'UNE CHARGE
  // ============================================================

  confirmerRetraitCharge(charge: ChargeDeclarationAffichee): void {

    this.confirmationService.confirm({

      header: 'Retirer cette charge',

      message: `Voulez-vous retirer la charge « ${charge.descriptionCharge} » de cette déclaration ?`,

      icon: 'pi pi-exclamation-triangle',

      acceptLabel: 'Retirer',
      rejectLabel: 'Annuler',

      acceptButtonStyleClass: 'p-button-danger',
      rejectButtonStyleClass: 'p-button-secondary',

      accept: () => this.retirerCharge(charge)
    });
  }

  private retirerCharge(charge: ChargeDeclarationAffichee): void {

    this.calculFiscalService
      .retirerCharge(charge.id)
      .subscribe({

        next: () => {

          this.charges = this.charges.filter(
            c => c.id !== charge.id
          );

          this.resultat = null;

          this.messageService.add({
            severity: 'success',
            summary: 'Charge retirée',
            detail: 'La charge a été retirée de la déclaration.'
          });

          this.chargerChargesDisponibles();
        },

        error: (erreur: HttpErrorResponse) => {

          this.messageService.add({
            severity: 'error',
            summary: 'Erreur de suppression',
            detail: this.obtenirMessageErreur(
              erreur,
              'Impossible de retirer cette charge.'
            ),
            life: 5000
          });
        }
      });
  }

  // ============================================================
  // LANCEMENT DU CALCUL FISCAL
  // ============================================================

  calculer(): void {

    if (!this.declarationId || this.enCoursCalcul) {
      return;
    }

    if (!this.estDeclarationRevenusFonciers) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Calcul non disponible',
        detail: 'Le calcul automatique actuellement disponible concerne les revenus fonciers.'
      });

      return;
    }

    if (!this.declarationGlobaleSociete) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Périmètre incorrect',
        detail: 'La déclaration des revenus fonciers doit être globale à la société, sans immeuble spécifique.'
      });

      return;
    }

    this.enCoursCalcul = true;

    this.calculFiscalService
      .calculer(this.declarationId)
      .subscribe({

        next: resultat => {

          // Conserver immédiatement la réponse du calcul.
          this.resultat = resultat;

          this.enCoursCalcul = false;

          // Recharger la déclaration sans effacer le résultat.
          this.chargerDeclaration(true);

          this.messageService.add({
            severity: 'success',
            summary: 'Calcul terminé',
            detail: resultat.message ||
              'Le calcul fiscal global de la société a été effectué avec succès.'
          });
        },

        error: (erreur: HttpErrorResponse) => {

          this.enCoursCalcul = false;

          this.messageService.add({
            severity: 'error',
            summary: 'Erreur de calcul',
            detail: this.obtenirMessageErreur(
              erreur,
              'Le calcul fiscal n’a pas pu être effectué.'
            ),
            life: 6000
          });
        }
      });
  }

  // ============================================================
  // ANNULATION D'UNE MODIFICATION LOCALE
  // ============================================================

  annulerModification(charge: ChargeDeclarationAffichee): void {
    charge.montantRetenuModifie = charge.montantRetenu;
  }

  // ============================================================
  // LIBELLÉS ET STATUTS
  // ============================================================

  getModeDeductionLibelle(
    mode: ModeDeductionCharges | null | undefined
  ): string {

    if (mode === ModeDeductionCharges.Forfait30Pourcent) {
      return 'Forfait de 30 %';
    }

    if (mode === ModeDeductionCharges.FraisReels) {
      return 'Frais réels';
    }

    return 'Non défini';
  }

  getStatutDeductibiliteLibelle(
    statut: StatutDeductibilite | number
  ): string {

    switch (statut) {

      case StatutDeductibilite.AQualifier:
        return 'À qualifier';

      case StatutDeductibilite.Deductible:
        return 'Déductible';

      case StatutDeductibilite.NonDeductible:
        return 'Non déductible';

      case StatutDeductibilite.PartiellementDeductible:
        return 'Partiellement déductible';

      default:
        return 'Inconnu';
    }
  }

  getStatutDeductibiliteSeverity(
    statut: StatutDeductibilite | number
  ): 'success' | 'warn' | 'danger' | 'secondary' {

    switch (statut) {

      case StatutDeductibilite.Deductible:
        return 'success';

      case StatutDeductibilite.PartiellementDeductible:
        return 'warn';

      case StatutDeductibilite.NonDeductible:
        return 'danger';

      default:
        return 'secondary';
    }
  }

  // ============================================================
  // VALEURS AFFICHÉES
  // ============================================================

  get totalLoyersBruts(): number {

    return this.resultat?.totalLoyersBruts
      ?? this.declaration?.totalLoyersBruts
      ?? 0;
  }

  get chargesForfaitaires(): number {

    return this.resultat?.chargesForfaitaires
      ?? this.declaration?.chargesForfaitaires
      ?? 0;
  }

  get chargesReellesDeclarees(): number {

    return this.resultat?.chargesReellesDeclarees
      ?? this.declaration?.chargesReellesDeclarees
      ?? 0;
  }

  get chargesReellesAdmissibles(): number {

    return this.resultat?.chargesReellesAdmissibles
      ?? this.declaration?.chargesReellesAdmissibles
      ?? 0;
  }

  get revenuNetFoncier(): number {

    return this.resultat?.revenuNetFoncier
      ?? this.declaration?.revenuNetFoncier
      ?? 0;
  }

  get baseImposable(): number {

    return this.resultat?.baseImposable
      ?? this.declaration?.baseImposable
      ?? 0;
  }

  get montantImpot(): number {

    return this.resultat?.montantImpot
      ?? this.declaration?.montantImpot
      ?? 0;
  }

  get acomptesVerses(): number {

    return this.resultat?.acomptesVerses
      ?? this.declaration?.acomptesVerses
      ?? 0;
  }

  get soldeImpot(): number {

    return this.resultat?.soldeImpot
      ?? this.declaration?.soldeImpot
      ?? 0;
  }

  get montantImpotPrincipal(): number {
  return this.resultat?.montantImpotPrincipal ?? 0;
}

get tauxCac(): number {
  return this.resultat?.tauxCac ?? 0;
}

get montantCac(): number {
  return this.resultat?.montantCac ?? 0;
}

  get modeDeduction(): ModeDeductionCharges | null {

    return this.resultat?.modeDeduction
      ?? this.declaration?.modeDeduction
      ?? null;
  }

  get nombreCharges(): number {
    return this.charges.length;
  }

  // ============================================================
  // GESTION DES ERREURS HTTP
  // ============================================================

  private obtenirMessageErreur(
    erreur: HttpErrorResponse,
    messageDefaut: string
  ): string {

    if (
      typeof erreur.error === 'string' &&
      erreur.error.trim()
    ) {
      return erreur.error;
    }

    if (erreur.error?.message) {
      return erreur.error.message;
    }

    if (erreur.error?.title) {
      return erreur.error.title;
    }

    if (erreur.status === 0) {
      return 'Impossible de joindre le serveur. Vérifie ta connexion.';
    }

    return messageDefaut;
  }
}
