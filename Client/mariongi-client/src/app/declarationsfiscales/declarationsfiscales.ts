import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { MessageService, ConfirmationService } from 'primeng/api';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TextareaModule } from 'primeng/textarea';
import { DatePickerModule } from 'primeng/datepicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { TooltipModule } from 'primeng/tooltip';

import {
  BienDeclarationFiscaleDto,
  DeclarationFiscaleDto,
  DeclarationFiscaleRequest,
  StatutDeclarationFiscale,
  TypeImpotFiscal,
  OptionDeductionFiscaleDto,
  ModeDeductionCharges,
  MODES_DEDUCTION_OPTIONS
} from '../models/gestimmo.models';

import { DeclarationsFiscalesService } from '../services/declarations-fiscales.service';
import { BiensService } from '../services/biens.service';
import { OptionsDeductionFiscalesService } from '../services/options-deduction-fiscales.service';

@Component({
  selector: 'app-declarationsfiscales',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TooltipModule,
    ReactiveFormsModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    SelectModule,
    TagModule,
    ToastModule,
    ConfirmDialogModule,
    TextareaModule,
    DatePickerModule,
    InputNumberModule
  ],
  templateUrl: './declarationsfiscales.html',
  styleUrl: './declarationsfiscales.scss',
  providers: [MessageService, ConfirmationService]
})
export class DeclarationsFiscales implements OnInit {

  private readonly biensService = inject(BiensService);
  private readonly router = inject(Router);

  // ============================================================
  // DONNÉES
  // ============================================================

  declarations: DeclarationFiscaleDto[] = [];

  biens: BienDeclarationFiscaleDto[] = [];

  declarationsFiltrees: DeclarationFiscaleDto[] = [];

  optionsDeductionFiscales: OptionDeductionFiscaleDto[] = [];

  chargement = false;
  chargementBiens = false;
  erreurChargementBiens = false;
  chargementOptionsDeduction = false;

  sauvegardeEnCours = false;
  suppressionEnCours = false;

  dialogueVisible = false;
  modeEdition = false;

  declarationSelectionnee: DeclarationFiscaleDto | null = null;

  recherche = '';
  filtreAnnee: number | null = null;
  filtreType: TypeImpotFiscal | null = null;
  filtreStatut: StatutDeclarationFiscale | null = null;

  readonly anneeCourante = new Date().getFullYear();

  readonly modesDeduction = MODES_DEDUCTION_OPTIONS;

  private initialisationFormulaire = false;

  formulaire: FormGroup;

  // ============================================================
  // TYPES D'IMPÔT
  // ============================================================

  readonly typesImpot = [
    { label: 'Non défini', value: TypeImpotFiscal.NonDefini },
    { label: 'Propriété foncière', value: TypeImpotFiscal.ProprieteFonciere },
    { label: 'Revenus fonciers', value: TypeImpotFiscal.RevenusFonciers },
    { label: 'Précompte sur loyers', value: TypeImpotFiscal.PrecompteSurLoyers },
    { label: 'IRPP', value: TypeImpotFiscal.IRPP },
    { label: 'Impôt sur les sociétés', value: TypeImpotFiscal.ImpotSocietes },
    { label: 'Plus-value immobilière', value: TypeImpotFiscal.PlusValueImmobiliere },
    { label: "Droits d'enregistrement", value: TypeImpotFiscal.DroitsEnregistrement },
    { label: 'TVA', value: TypeImpotFiscal.TVA },
    { label: 'Autre', value: TypeImpotFiscal.Autre }
  ];

  // ============================================================
  // STATUTS
  // ============================================================

  readonly statuts = [
    { label: 'À préparer', value: StatutDeclarationFiscale.APreparer },
    { label: 'À déclarer', value: StatutDeclarationFiscale.ADeclarer },
    { label: 'Déclarée', value: StatutDeclarationFiscale.Declaree },
    { label: 'À régler', value: StatutDeclarationFiscale.ARegler },
    {
      label: 'Partiellement réglée',
      value: StatutDeclarationFiscale.PartiellementReglee
    },
    { label: 'Réglée', value: StatutDeclarationFiscale.Reglee },
    { label: 'En retard', value: StatutDeclarationFiscale.EnRetard }
  ];

  // ============================================================
  // CONSTRUCTEUR
  // ============================================================

  constructor(
    private readonly fb: FormBuilder,
    private readonly service: DeclarationsFiscalesService,
    private readonly messageService: MessageService,
    private readonly confirmationService: ConfirmationService,
    private readonly optionsDeductionService: OptionsDeductionFiscalesService
  ) {

    this.formulaire = this.fb.group({

      bienImmobilierId: [null as string | null],

      optionDeductionFiscaleId: [null as string | null],

      anneeFiscale: [
        this.anneeCourante,
        [
          Validators.required,
          Validators.min(1900),
          Validators.max(2100),
          Validators.pattern(/^\d{4}$/)
        ]
      ],

      typeImpot: [
        TypeImpotFiscal.NonDefini,
        [Validators.required, Validators.min(1)]
      ],

      baseImposable: [
        null as number | null,
        [Validators.min(0)]
      ],

      montantImpot: [
        0,
        [Validators.required, Validators.min(0)]
      ],

      dateEcheance: [null as Date | null],

      dateDeclaration: [null as Date | null],

      referenceDeclaration: [
        '',
        [Validators.maxLength(150)]
      ],

      observations: [
        '',
        [Validators.maxLength(2000)]
      ],

      statut: [
        StatutDeclarationFiscale.APreparer,
        [Validators.required, Validators.min(1)]
      ]
    });
  }

  // ============================================================
  // PROPRIÉTÉS CALCULÉES POUR LE FORMULAIRE
  // ============================================================

  get estDeclarationRevenusFonciers(): boolean {

    return Number(this.formulaire.get('typeImpot')?.value) ===
      TypeImpotFiscal.RevenusFonciers;
  }

  get estDeclarationProprieteFonciere(): boolean {

    return Number(this.formulaire.get('typeImpot')?.value) ===
      TypeImpotFiscal.ProprieteFonciere;
  }

  /**
   * Pour les revenus fonciers, la base imposable et le montant
   * de l'impôt proviennent du calcul fiscal.
   */
  get estChampFiscalCalcule(): boolean {

    return this.estDeclarationRevenusFonciers;
  }

  // ============================================================
  // INITIALISATION
  // ============================================================

  ngOnInit(): void {

    this.chargerDeclarations();

    this.chargerBiens();

    this.formulaire.get('anneeFiscale')?.valueChanges.subscribe(
      (annee: number | null) => {

        if (this.initialisationFormulaire) {
          return;
        }

        this.changerAnneeFiscale(
          annee === null ? null : Number(annee)
        );
      }
    );

    this.formulaire.get('typeImpot')?.valueChanges.subscribe(
      (type: number | null) => {

        if (this.initialisationFormulaire) {
          return;
        }

        this.appliquerReglesTypeImpot(Number(type));
      }
    );
  }

  // ============================================================
  // RÈGLES MÉTIER SELON LE TYPE D'IMPÔT
  // ============================================================

private appliquerReglesTypeImpot(typeImpot: number): void {

  const bienControl = this.formulaire.get('bienImmobilierId');
  const optionControl = this.formulaire.get('optionDeductionFiscaleId');

  const baseControl = this.formulaire.get('baseImposable');
  const impotControl = this.formulaire.get('montantImpot');

  if (!bienControl || !optionControl) {
    return;
  }

  if (typeImpot === TypeImpotFiscal.RevenusFonciers) {

    baseControl?.disable({ emitEvent: false });
    impotControl?.disable({ emitEvent: false });

    bienControl.setValue(null, { emitEvent: false });
    bienControl.clearValidators();

    optionControl.setValidators([Validators.required]);

    const annee = Number(
      this.formulaire.get('anneeFiscale')?.value
    );

    if (this.optionsDeductionFiscales.length > 0) {

      // Une seule option existe normalement par société et année.
      if (this.optionsDeductionFiscales.length === 1) {

        optionControl.setValue(
          this.optionsDeductionFiscales[0].id,
          { emitEvent: false }
        );
      }

    } else if (!this.chargementOptionsDeduction) {

      this.chargerOptionsDeductionFiscales(annee);
    }

  } else {

    baseControl?.enable({ emitEvent: false });
    impotControl?.enable({ emitEvent: false });

    optionControl.setValue(null, { emitEvent: false });
    optionControl.clearValidators();

    if (typeImpot === TypeImpotFiscal.ProprieteFonciere) {

      bienControl.setValidators([Validators.required]);

    } else {

      bienControl.clearValidators();
    }
  }

  bienControl.updateValueAndValidity({ emitEvent: false });
  optionControl.updateValueAndValidity({ emitEvent: false });

  baseControl?.updateValueAndValidity({ emitEvent: false });
  impotControl?.updateValueAndValidity({ emitEvent: false });
}
  // ============================================================
  // RÉINITIALISATION CONTRÔLÉE DU FORMULAIRE
  // ============================================================

  private reinitialiserFormulaire(
    valeurs: Record<string, unknown>
  ): void {

    this.initialisationFormulaire = true;

    this.formulaire.reset(valeurs);

    this.initialisationFormulaire = false;

    this.formulaire.markAsPristine();
    this.formulaire.markAsUntouched();

    this.formulaire.updateValueAndValidity();

    this.appliquerReglesTypeImpot(
      Number(this.formulaire.get('typeImpot')?.value)
    );
  }

  // ============================================================
  // CHARGEMENT DES OPTIONS DE DÉDUCTION FISCALE
  // ============================================================

private chargerOptionsDeductionFiscales(
  anneeFiscale: number,
  optionSelectionneeId?: string | null
): void {
  this.chargementOptionsDeduction = true;

  this.optionsDeductionService.getAll(anneeFiscale).subscribe({
    next: (options) => {
      this.optionsDeductionFiscales = options ?? [];

      const controle = this.formulaire.get('optionDeductionFiscaleId');

      if (optionSelectionneeId) {
        // En modification, conserver l'option déjà associée à la déclaration.
        const optionExiste = this.optionsDeductionFiscales.some(
          option => option.id === optionSelectionneeId
        );

        controle?.setValue(
          optionExiste ? optionSelectionneeId : null,
          { emitEvent: false }
        );

        if (!optionExiste) {
          this.messageService.add({
            severity: 'warn',
            summary: 'Option introuvable',
            detail: 'L’option de déduction fiscale associée à cette déclaration n’existe plus pour cette année fiscale.'
          });
        }
      } else if (
        this.estDeclarationRevenusFonciers &&
        this.optionsDeductionFiscales.length === 1
      ) {
        // En création, sélectionner automatiquement l'unique option disponible.
        controle?.setValue(
          this.optionsDeductionFiscales[0].id,
          { emitEvent: false }
        );
      }

      this.chargementOptionsDeduction = false;

      controle?.updateValueAndValidity({ emitEvent: false });
    },

    error: (erreur: unknown) => {
      this.optionsDeductionFiscales = [];
      this.chargementOptionsDeduction = false;

      this.formulaire.get('optionDeductionFiscaleId')?.setValue(
        null,
        { emitEvent: false }
      );

      this.formulaire.get('optionDeductionFiscaleId')?.updateValueAndValidity({
        emitEvent: false
      });

      console.error(
        'Erreur lors du chargement des options de déduction fiscale :',
        erreur
      );

      this.messageService.add({
        severity: 'error',
        summary: 'Erreur',
        detail: 'Impossible de charger les options de déduction fiscale.'
      });
    }
  });
}

  // ============================================================
  // OUVERTURE DU CALCUL FISCAL
  // ============================================================

  ouvrirCalcul(declaration: DeclarationFiscaleDto): void {

    if (!declaration?.id) {

      this.messageService.add({
        severity: 'error',
        summary: 'Erreur',
        detail:
          'Impossible d’ouvrir le calcul : identifiant de déclaration manquant.'
      });

      return;
    }

    this.router.navigate([
      '/calculfiscal',
      declaration.id
    ]);
  }

  // ============================================================
  // CHANGEMENT D'ANNÉE FISCALE
  // ============================================================

  changerAnneeFiscale(annee: number | null): void {

    if (this.initialisationFormulaire) {
      return;
    }

    this.formulaire.patchValue({
      optionDeductionFiscaleId: null
    }, {
      emitEvent: false
    });

    this.optionsDeductionFiscales = [];

    if (annee !== null) {
      this.chargerOptionsDeductionFiscales(annee);
    }
  }

  // ============================================================
  // LIBELLÉ DU MODE DE DÉDUCTION
  // ============================================================

  libelleModeDeduction(
    mode: ModeDeductionCharges | null
  ): string {

    if (mode === null) {
      return 'Non défini';
    }

    return this.modesDeduction.find(
      element => element.value === mode
    )?.label ?? 'Inconnu';
  }

  // ============================================================
  // CHARGEMENT DES DÉCLARATIONS
  // ============================================================

  chargerDeclarations(): void {

    if (this.chargement) {
      return;
    }

    this.chargement = true;

    this.service.getAll().subscribe({

      next: (resultat) => {

        this.declarations = Array.isArray(resultat)
          ? resultat
          : [];

        this.appliquerFiltres();

        this.chargement = false;

        if (!Array.isArray(resultat)) {

          this.messageService.add({
            severity: 'error',
            summary: 'Format de réponse invalide',
            detail:
              'L’API des déclarations fiscales ne renvoie pas une liste.'
          });
        }
      },

      error: (erreur: HttpErrorResponse) => {

        this.chargement = false;

        this.afficherErreur(
          erreur,
          'Impossible de charger les déclarations fiscales.'
        );
      }
    });
  }

  // ============================================================
  // CHARGEMENT DES BIENS IMMOBILIERS
  // ============================================================

  chargerBiens(): void {

    this.chargementBiens = true;
    this.erreurChargementBiens = false;

    this.biensService.getBiens().subscribe({

      next: (biens) => {

        this.biens = Array.isArray(biens) ? biens : [];

        this.chargementBiens = false;

        if (!Array.isArray(biens)) {

          this.erreurChargementBiens = true;

          this.messageService.add({
            severity: 'error',
            summary: 'Format de réponse invalide',
            detail:
              'L’API des biens immobiliers ne renvoie pas une liste.'
          });

          return;
        }

        if (this.biens.length === 0) {

          this.messageService.add({
            severity: 'warn',
            summary: 'Aucun bien disponible',
            detail:
              'Aucun bien immobilier n’a été retourné par l’API.'
          });
        }
      },

      error: (erreur: HttpErrorResponse) => {

        this.biens = [];
        this.chargementBiens = false;
        this.erreurChargementBiens = true;

        this.afficherErreur(
          erreur,
          'Impossible de charger les biens immobiliers.'
        );
      }
    });
  }

  // ============================================================
  // RECHERCHE ET FILTRES
  // ============================================================

  appliquerFiltres(): void {

    const terme = this.recherche
      .trim()
      .toLocaleLowerCase('fr-FR');

    this.declarationsFiltrees = this.declarations.filter(
      (declaration) => {

        const bien = declaration.bienImmobilier;

        const texte = [
          bien?.nom,
          bien?.reference,
          bien?.adresse,
          bien?.ville,
          declaration.referenceDeclaration,
          declaration.observations,
          this.libelleType(declaration.typeImpot),
          this.libelleStatut(declaration.statut)
        ]
          .filter(
            valeur => valeur !== null && valeur !== undefined
          )
          .join(' ')
          .toLocaleLowerCase('fr-FR');

        return (
          (!terme || texte.includes(terme)) &&
          (
            this.filtreAnnee === null ||
            declaration.anneeFiscale === this.filtreAnnee
          ) &&
          (
            this.filtreType === null ||
            declaration.typeImpot === this.filtreType
          ) &&
          (
            this.filtreStatut === null ||
            declaration.statut === this.filtreStatut
          )
        );
      }
    );
  }

  reinitialiserFiltres(): void {

    this.recherche = '';
    this.filtreAnnee = null;
    this.filtreType = null;
    this.filtreStatut = null;

    this.appliquerFiltres();
  }

  // ============================================================
  // OUVERTURE DU FORMULAIRE DE CRÉATION
  // ============================================================

  ouvrirCreation(): void {

    if (this.sauvegardeEnCours) {
      return;
    }

    this.modeEdition = false;
    this.declarationSelectionnee = null;

    this.reinitialiserFormulaire({

      bienImmobilierId: null,
      optionDeductionFiscaleId: null,
      anneeFiscale: this.anneeCourante,
      typeImpot: TypeImpotFiscal.NonDefini,
      baseImposable: null,
      montantImpot: 0,
      dateEcheance: null,
      dateDeclaration: null,
      referenceDeclaration: '',
      observations: '',
      statut: StatutDeclarationFiscale.APreparer
    });

    this.chargerOptionsDeductionFiscales(this.anneeCourante);

    this.dialogueVisible = true;
  }

  // ============================================================
  // OUVERTURE DU FORMULAIRE DE MODIFICATION
  // ============================================================

  ouvrirModification(declaration: DeclarationFiscaleDto): void {

    if (this.sauvegardeEnCours) {
      return;
    }

    this.modeEdition = true;
    this.declarationSelectionnee = declaration;

    this.reinitialiserFormulaire({

      bienImmobilierId: declaration.bienImmobilierId ?? null,

      optionDeductionFiscaleId:
        declaration.optionDeductionFiscaleId ?? null,

      anneeFiscale: declaration.anneeFiscale,

      typeImpot: declaration.typeImpot,

      baseImposable: declaration.baseImposable ?? null,

      montantImpot: declaration.montantImpot,

      dateEcheance: this.versDate(declaration.dateEcheance),

      dateDeclaration: this.versDate(declaration.dateDeclaration),

      referenceDeclaration:
        declaration.referenceDeclaration ?? '',

      observations:
        declaration.observations ?? '',

      statut: declaration.statut
    });

    this.chargerOptionsDeductionFiscales(
      declaration.anneeFiscale,
      declaration.optionDeductionFiscaleId ?? null
    );

    this.dialogueVisible = true;
  }

  // ============================================================
  // ENREGISTREMENT
  // ============================================================

  enregistrer(): void {

    this.formulaire.markAllAsTouched();
    this.formulaire.updateValueAndValidity();

    if (this.sauvegardeEnCours) {
      return;
    }

    if (this.formulaire.invalid) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Formulaire invalide',
        detail:
          'Vérifie les champs obligatoires et corrige les erreurs avant de continuer.'
      });

      return;
    }

    const valeur = this.formulaire.getRawValue();

    const anneeFiscale = Number(valeur.anneeFiscale);
    const typeImpot = Number(valeur.typeImpot);

    const estRevenusFonciers =
      typeImpot === TypeImpotFiscal.RevenusFonciers;

    const montantImpot = Number(valeur.montantImpot);

    const baseImposable =
      valeur.baseImposable === null ||
      valeur.baseImposable === '' ||
      valeur.baseImposable === undefined
        ? null
        : Number(valeur.baseImposable);

    // ----------------------------------------------------------
    // VÉRIFICATION DE L'ANNÉE FISCALE
    // ----------------------------------------------------------

    if (
      !Number.isInteger(anneeFiscale) ||
      anneeFiscale < 1900 ||
      anneeFiscale > 2100
    ) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Année fiscale invalide',
        detail:
          'L’année fiscale doit être comprise entre 1900 et 2100.'
      });

      return;
    }

    // ----------------------------------------------------------
    // VÉRIFICATION DES MONTANTS
    // ----------------------------------------------------------

    if (
      !Number.isFinite(montantImpot) ||
      montantImpot < 0 ||
      (
        baseImposable !== null &&
        (!Number.isFinite(baseImposable) || baseImposable < 0)
      )
    ) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Montant invalide',
        detail:
          'Les montants doivent être des nombres positifs ou nuls.'
      });

      return;
    }

    // ----------------------------------------------------------
    // VÉRIFICATION DU TYPE D'IMPÔT
    // ----------------------------------------------------------

    if (
      !this.typesImpot.some(
        type =>
          type.value === typeImpot &&
          type.value !== TypeImpotFiscal.NonDefini
      )
    ) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Type d’impôt invalide',
        detail:
          'Sélectionne un type d’impôt valide.'
      });

      return;
    }

    // ----------------------------------------------------------
    // DÉTERMINATION DU PÉRIMÈTRE
    // ----------------------------------------------------------

    const estProprieteFonciere =
      typeImpot === TypeImpotFiscal.ProprieteFonciere;

    if (
      estProprieteFonciere &&
      !valeur.bienImmobilierId
    ) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Bien immobilier obligatoire',
        detail:
          'Sélectionne le bien immobilier concerné par cette déclaration.'
      });

      return;
    }

    const bienImmobilierId = estRevenusFonciers
      ? null
      : valeur.bienImmobilierId || null;

    // ----------------------------------------------------------
    // VÉRIFICATION DE L'OPTION DE DÉDUCTION
    // ----------------------------------------------------------

    const optionDeductionFiscaleId =
      estRevenusFonciers
        ? valeur.optionDeductionFiscaleId || null
        : null;

    if (
      estRevenusFonciers &&
      !optionDeductionFiscaleId
    ) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Option de déduction obligatoire',
        detail:
          'Pour une déclaration de revenus fonciers, sélectionne d’abord une option de déduction fiscale pour cette année.'
      });

      return;
    }

    if (optionDeductionFiscaleId) {

      const optionSelectionnee = this.optionsDeductionFiscales.find(
        option => option.id === optionDeductionFiscaleId
      );

      if (
        !optionSelectionnee ||
        optionSelectionnee.anneeFiscale !== anneeFiscale
      ) {

        this.messageService.add({
          severity: 'warn',
          summary: 'Option de déduction invalide',
          detail:
            'L’option sélectionnée ne correspond pas à l’année fiscale de la déclaration.'
        });

        return;
      }
    }

    // ----------------------------------------------------------
    // PROTECTION DES MONTANTS CALCULÉS
    // ----------------------------------------------------------

    let baseImposableFinale = baseImposable;
    let montantImpotFinal = montantImpot;

    if (estRevenusFonciers && this.modeEdition && this.declarationSelectionnee) {

      // On conserve les montants enregistrés par le calcul fiscal.
      baseImposableFinale =
        this.declarationSelectionnee.baseImposable ?? null;

      montantImpotFinal =
        this.declarationSelectionnee.montantImpot;
    }

    // ----------------------------------------------------------
    // CONSTRUCTION DE LA REQUÊTE
    // ----------------------------------------------------------

    const request: DeclarationFiscaleRequest = {

      bienImmobilierId,

      anneeFiscale,

      typeImpot: typeImpot as TypeImpotFiscal,

      baseImposable: baseImposableFinale,

      montantImpot: montantImpotFinal,

      dateEcheance: this.versIso(valeur.dateEcheance),

      dateDeclaration: this.versIso(valeur.dateDeclaration),

      referenceDeclaration: this.texteOptionnel(
        valeur.referenceDeclaration
      ),

      observations: this.texteOptionnel(
        valeur.observations
      ),

      statut: Number(
        valeur.statut
      ) as StatutDeclarationFiscale,

      optionDeductionFiscaleId
    };

    this.sauvegardeEnCours = true;

    const estModification =
      this.modeEdition &&
      this.declarationSelectionnee !== null;

    const requete = estModification
      ? this.service.update(
          this.declarationSelectionnee!.id,
          request
        )
      : this.service.create(request);

    requete.subscribe({

      next: () => {

        this.sauvegardeEnCours = false;
        this.dialogueVisible = false;

        this.messageService.add({
          severity: 'success',
          summary: 'Succès',
          detail: estModification
            ? 'La déclaration fiscale a été modifiée.'
            : 'La déclaration fiscale a été créée.'
        });

        this.chargerDeclarations();
      },

      error: (erreur: HttpErrorResponse) => {

        this.sauvegardeEnCours = false;

        this.afficherErreur(
          erreur,
          'Impossible d’enregistrer la déclaration fiscale.'
        );
      }
    });
  }

  // ============================================================
  // SUPPRESSION
  // ============================================================

  confirmerSuppression(declaration: DeclarationFiscaleDto): void {

    if (
      this.suppressionEnCours ||
      this.sauvegardeEnCours
    ) {
      return;
    }

    this.confirmationService.confirm({

      header: 'Confirmation de suppression',

      message:
        `Voulez-vous supprimer la déclaration fiscale de ${declaration.anneeFiscale} ?`,

      icon: 'pi pi-exclamation-triangle',

      acceptLabel: 'Supprimer',
      rejectLabel: 'Annuler',

      acceptButtonStyleClass: 'p-button-danger',

      accept: () => this.supprimer(declaration)
    });
  }

  private supprimer(declaration: DeclarationFiscaleDto): void {

    if (this.suppressionEnCours) {
      return;
    }

    this.suppressionEnCours = true;

    this.service.delete(declaration.id).subscribe({

      next: () => {

        this.suppressionEnCours = false;

        this.messageService.add({
          severity: 'success',
          summary: 'Succès',
          detail: 'La déclaration fiscale a été supprimée.'
        });

        this.chargerDeclarations();
      },

      error: (erreur: HttpErrorResponse) => {

        this.suppressionEnCours = false;

        this.afficherErreur(
          erreur,
          'Impossible de supprimer la déclaration fiscale.'
        );
      }
    });
  }

  // ============================================================
  // LIBELLÉS ET PRÉSENTATION
  // ============================================================

  libelleType(type: TypeImpotFiscal): string {

    return (
      this.typesImpot.find(
        element => element.value === type
      )?.label ?? 'Inconnu'
    );
  }

  libelleStatut(statut: StatutDeclarationFiscale): string {

    return (
      this.statuts.find(
        element => element.value === statut
      )?.label ?? 'Inconnu'
    );
  }

  severiteStatut(
    statut: StatutDeclarationFiscale
  ): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {

    switch (statut) {

      case StatutDeclarationFiscale.Declaree:
      case StatutDeclarationFiscale.Reglee:
        return 'success';

      case StatutDeclarationFiscale.ADeclarer:
      case StatutDeclarationFiscale.APreparer:
        return 'info';

      case StatutDeclarationFiscale.ARegler:
      case StatutDeclarationFiscale.PartiellementReglee:
        return 'warn';

      case StatutDeclarationFiscale.EnRetard:
        return 'danger';

      default:
        return 'secondary';
    }
  }

  libelleBien(declaration: DeclarationFiscaleDto): string {

    if (!declaration.bienImmobilier) {
      return 'Tous les biens / propriétaire';
    }

    const bien = declaration.bienImmobilier;

    return `${bien.nom} (${bien.reference})`;
  }

  formaterMontant(montant: number | null | undefined): string {

    if (montant === null || montant === undefined) {
      return '—';
    }

    return (
      new Intl.NumberFormat('fr-FR', {
        minimumFractionDigits: 0,
        maximumFractionDigits: 2
      }).format(montant) + ' FCFA'
    );
  }

  formaterDate(date: string | null): string {

    if (!date) {
      return '—';
    }

    const valeur = this.versDate(date);

    return valeur
      ? new Intl.DateTimeFormat('fr-FR').format(valeur)
      : '—';
  }

  // ============================================================
  // GESTION DES DATES
  // ============================================================

  private versDate(
    value: string | Date | null | undefined
  ): Date | null {

    if (!value) {
      return null;
    }

    const date = value instanceof Date
      ? value
      : new Date(value);

    return Number.isNaN(date.getTime())
      ? null
      : date;
  }

  private versIso(
    value: Date | string | null | undefined
  ): string | null {

    if (!value) {
      return null;
    }

    const date = this.versDate(value);

    if (!date) {
      return null;
    }

    const annee = date.getFullYear();

    const mois = String(
      date.getMonth() + 1
    ).padStart(2, '0');

    const jour = String(
      date.getDate()
    ).padStart(2, '0');

    return `${annee}-${mois}-${jour}T00:00:00`;
  }

  // ============================================================
  // INDICATEURS
  // ============================================================

  get nombreDeclarationsAPreparer(): number {

    return this.declarations.filter(
      declaration =>
        declaration.statut === StatutDeclarationFiscale.APreparer
    ).length;
  }

  get nombreDeclarationsADeclarer(): number {

    return this.declarations.filter(
      declaration =>
        declaration.statut === StatutDeclarationFiscale.ADeclarer
    ).length;
  }

  get nombreDeclarationsDeclarees(): number {

    return this.declarations.filter(
      declaration =>
        declaration.statut === StatutDeclarationFiscale.Declaree
    ).length;
  }

  get nombreDeclarationsARegler(): number {

    return this.declarations.filter(
      declaration =>
        declaration.statut === StatutDeclarationFiscale.ARegler
    ).length;
  }

  get nombreDeclarationsPartiellementReglees(): number {

    return this.declarations.filter(
      declaration =>
        declaration.statut === StatutDeclarationFiscale.PartiellementReglee
    ).length;
  }

  get nombreDeclarationsReglees(): number {

    return this.declarations.filter(
      declaration =>
        declaration.statut === StatutDeclarationFiscale.Reglee
    ).length;
  }

  get nombreDeclarationsEnRetard(): number {

    return this.declarations.filter(
      declaration =>
        declaration.statut === StatutDeclarationFiscale.EnRetard
    ).length;
  }

  // ============================================================
  // UTILITAIRES
  // ============================================================

  private texteOptionnel(
    value: string | null | undefined
  ): string | null {

    const texte = value?.trim();

    return texte ? texte : null;
  }

  private afficherErreur(
    erreur: HttpErrorResponse,
    messageParDefaut: string
  ): void {

    const message =
      erreur.error?.message ??
      erreur.error?.Message ??
      erreur.error?.title ??
      messageParDefaut;

    this.messageService.add({

      severity: 'error',

      summary:
        erreur.status === 403
          ? 'Accès refusé'
          : erreur.status === 401
            ? 'Authentification requise'
            : 'Erreur',

      detail: message
    });
  }
}
