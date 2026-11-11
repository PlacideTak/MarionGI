
import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { finalize, switchMap } from 'rxjs';

import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';

import {
  ChargeBienDto,
  CreerChargeBienRequest,
  ModifierChargeBienRequest,
  StatutDeductibilite,
  TypeCharge
} from '../models/gestimmo.models';

import { BiensService } from '../services/biens.service';
import { ChargesBiensService } from '../services/charges-biens.service';

interface OptionSelect {
  label: string;
  value: string | number | null;
}

interface UniteOption {
  id: string;
  reference?: string;
  nom?: string;
}

@Component({
  selector: 'app-charges-biens',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    SelectModule,
    TagModule,
    ToastModule,
    ConfirmDialogModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './chargesimmobilieres.html',
  styleUrl: './chargesimmobilieres.scss'
})
export class ChargesImmobilieres implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly chargesService = inject(ChargesBiensService);
  private readonly biensService = inject(BiensService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly StatutDeductibilite = StatutDeductibilite;

  statutsDeductibilite = this.chargesService.statutsDeductibilite;

  charges: ChargeBienDto[] = [];
  chargesFiltrees: ChargeBienDto[] = [];

  biensOptions: OptionSelect[] = [];
  unitesOptions: OptionSelect[] = [];

  typesCharges = this.chargesService.typesCharges;

  chargement = false;
  chargementBiens = false;
  enregistrement = false;
  suppressionEnCours = false;

  displayModal = false;
  isEditMode = false;
  selectedChargeId: string | null = null;

  recherche = '';

  filtreBienId: string | null = null;
  filtreUniteId: string | null = null;
  filtreTypeCharge: TypeCharge | null = null;
  filtreDateDebut = '';
  filtreDateFin = '';

  readonly aujourdHui = this.formatDateInput(new Date());

  chargeForm: FormGroup = this.fb.group({
    bienImmobilierId: ['', Validators.required],
    uniteLocativeId: [null],

    dateDepense: [this.aujourdHui, Validators.required],
    dateFacture: [null],
    datePaiement: [null],

    statutDeductibilite: [
      StatutDeductibilite.AQualifier,
      Validators.required
    ],

    montantDeductibleFiscalement: [null],

    justificationNonDeductibilite: [
      '',
      Validators.maxLength(2000)
    ],

    typeCharge: [TypeCharge.Entretien, Validators.required],

    description: ['', [
      Validators.required,
      Validators.maxLength(2000)
    ]],

    nomFournisseur: ['', Validators.maxLength(200)],
    numeroContribuableFournisseur: ['', Validators.maxLength(100)],

    montantTTC: [null, [
      Validators.required,
      Validators.min(0.01)
    ]],

    montantHT: [null, Validators.min(0)],
    montantTVA: [null, Validators.min(0)],

    referenceFacture: ['', Validators.maxLength(100)],
    cheminJustificatif: ['', Validators.maxLength(500)],
    observations: ['', Validators.maxLength(2000)]
  });

  ngOnInit(): void {
    this.configurerValidationFiscale();

    this.chargerBiens();
    this.chargerCharges();
  }

  // ============================================================
  // VALIDATION FISCALE CONDITIONNELLE
  // ============================================================

  private configurerValidationFiscale(): void {
    this.chargeForm
      .get('statutDeductibilite')
      ?.valueChanges
      .subscribe(() => {
        this.appliquerValidationFiscale();
      });

    this.appliquerValidationFiscale();
  }

  private appliquerValidationFiscale(): void {
    const statut = Number(
      this.chargeForm.get('statutDeductibilite')?.value
    );

    const montant = this.chargeForm.get(
      'montantDeductibleFiscalement'
    );

    const justification = this.chargeForm.get(
      'justificationNonDeductibilite'
    );

    if (!montant || !justification) {
      return;
    }

    // Réinitialisation des validateurs.
    montant.clearValidators();
    justification.clearValidators();

    if (
      statut === StatutDeductibilite.Deductible ||
      statut === StatutDeductibilite.PartiellementDeductible
    ) {
      // Une charge déductible doit avoir un montant fiscal.
      montant.setValidators([
        Validators.required,
        Validators.min(0.01)
      ]);

      // La justification de non-déductibilité n'est pas applicable.
      justification.setValue('', { emitEvent: false });
    } else if (
      statut === StatutDeductibilite.NonDeductible
    ) {
      // Une charge non déductible doit être justifiée.
      justification.setValidators([
        Validators.required,
        Validators.maxLength(2000)
      ]);

      montant.setValue(null, { emitEvent: false });
    } else {
      // À qualifier : aucune qualification fiscale définitive.
      montant.setValue(null, { emitEvent: false });
      justification.setValue('', { emitEvent: false });
    }

    montant.updateValueAndValidity({ emitEvent: false });
    justification.updateValueAndValidity({ emitEvent: false });
  }

  // ============================================================
  // INDICATEURS
  // ============================================================

  get totalCharges(): number {
    return this.chargesFiltrees.length;
  }

  get montantTotalTTC(): number {
    return this.chargesFiltrees.reduce(
      (total, charge) =>
        total + Number(charge.montantTTC || 0),
      0
    );
  }

  get montantTotalDeductible(): number {
    return this.chargesFiltrees.reduce((total, charge) => {
      let montantDeductible = 0;

      switch (charge.statutDeductibilite) {
        case StatutDeductibilite.Deductible:
          montantDeductible =
            charge.montantDeductibleFiscalement ??
            charge.montantTTC;
          break;

        case StatutDeductibilite.PartiellementDeductible:
          montantDeductible =
            charge.montantDeductibleFiscalement ?? 0;
          break;

        case StatutDeductibilite.AQualifier:
        case StatutDeductibilite.NonDeductible:
        default:
          montantDeductible = 0;
          break;
      }

      return total + Number(montantDeductible || 0);
    }, 0);
  }

  get nombreBiensConcernes(): number {
    return new Set(
      this.chargesFiltrees.map(charge => charge.bienImmobilierId)
    ).size;
  }

  get f(): { [key: string]: AbstractControl } {
    return this.chargeForm.controls;
  }

  // ============================================================
  // CHARGEMENT DES BIENS
  // ============================================================

  private chargerBiens(): void {
    this.chargementBiens = true;

    this.biensService.getBiens()
      .pipe(
        finalize(() => {
          this.chargementBiens = false;
        })
      )
      .subscribe({
        next: (biens: any[]) => {
          this.biensOptions = (biens ?? []).map(bien => ({
            label: `${bien.nom || bien.reference || 'Bien immobilier'}${bien.ville ? ' — ' + bien.ville : ''}`,
            value: bien.id
          }));
        },
        error: () => {
          this.messageService.add({
            severity: 'error',
            summary: 'Erreur',
            detail: 'Impossible de charger les biens immobiliers.'
          });
        }
      });
  }

  // ============================================================
  // CHARGEMENT DES CHARGES
  // ============================================================

  chargerCharges(): void {
    this.chargement = true;

    this.chargesService.getCharges({
      bienImmobilierId: this.filtreBienId,
      uniteLocativeId: this.filtreUniteId,
      typeCharge: this.filtreTypeCharge,
      dateDebut: this.filtreDateDebut || null,
      dateFin: this.filtreDateFin || null
    })
      .pipe(
        finalize(() => {
          this.chargement = false;
        })
      )
      .subscribe({
        next: charges => {
          this.charges = charges ?? [];
          this.appliquerRechercheLocale();
        },
        error: erreur => {
          this.messageService.add({
            severity: 'error',
            summary: 'Erreur de chargement',
            detail: this.extraireMessageErreur(
              erreur,
              'Impossible de charger les charges des biens.'
            )
          });
        }
      });
  }

  // ============================================================
  // FILTRES
  // ============================================================

  appliquerFiltres(): void {
    if (
      this.filtreDateDebut &&
      this.filtreDateFin &&
      this.filtreDateDebut > this.filtreDateFin
    ) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Dates invalides',
        detail: 'La date de début doit précéder la date de fin.'
      });

      return;
    }

    this.chargerCharges();
  }

  reinitialiserFiltres(): void {
    this.recherche = '';

    this.filtreBienId = null;
    this.filtreUniteId = null;
    this.filtreTypeCharge = null;
    this.filtreDateDebut = '';
    this.filtreDateFin = '';

    this.chargerCharges();
  }

  rechercherLocalement(): void {
    this.appliquerRechercheLocale();
  }

  private appliquerRechercheLocale(): void {
    const terme = this.recherche.trim().toLocaleLowerCase();

    this.chargesFiltrees = this.charges.filter(charge => {
      if (!terme) {
        return true;
      }

      const texte = [
        charge.nomBienImmobilier,
        charge.referenceUnite,
        charge.description,
        charge.referenceFacture,
        charge.nomFournisseur,
        charge.numeroContribuableFournisseur,
        charge.justificationNonDeductibilite,
        this.getTypeChargeLibelle(charge.typeCharge),
        this.getStatutDeductibiliteLibelle(
          charge.statutDeductibilite
        )
      ]
        .filter(Boolean)
        .join(' ')
        .toLocaleLowerCase();

      return texte.includes(terme);
    });
  }

  // ============================================================
  // CRÉATION
  // ============================================================

  ouvrirCreation(): void {
    this.isEditMode = false;
    this.selectedChargeId = null;
    this.unitesOptions = [];

    this.chargeForm.reset({
      bienImmobilierId: '',
      uniteLocativeId: null,

      statutDeductibilite: StatutDeductibilite.AQualifier,
      montantDeductibleFiscalement: null,
      justificationNonDeductibilite: '',

      dateDepense: this.aujourdHui,
      dateFacture: null,
      datePaiement: null,

      typeCharge: TypeCharge.Entretien,
      description: '',

      nomFournisseur: '',
      numeroContribuableFournisseur: '',

      montantTTC: null,
      montantHT: null,
      montantTVA: null,

      referenceFacture: '',
      cheminJustificatif: '',
      observations: ''
    });

    this.appliquerValidationFiscale();

    this.chargeForm.markAsPristine();
    this.chargeForm.markAsUntouched();

    this.displayModal = true;
  }

  // ============================================================
  // MODIFICATION
  // ============================================================

  ouvrirModification(charge: ChargeBienDto): void {
    this.isEditMode = true;
    this.selectedChargeId = charge.id;

    this.chargeForm.reset({
      bienImmobilierId: charge.bienImmobilierId,
      uniteLocativeId: charge.uniteLocativeId,

      dateDepense: this.formatDateInput(charge.dateDepense),
      dateFacture: this.formatDateInput(charge.dateFacture),
      datePaiement: this.formatDateInput(charge.datePaiement),

      typeCharge: charge.typeCharge,
      description: charge.description,

      nomFournisseur: charge.nomFournisseur ?? '',
      numeroContribuableFournisseur:
        charge.numeroContribuableFournisseur ?? '',

      montantTTC: charge.montantTTC,
      montantHT: charge.montantHT,
      montantTVA: charge.montantTVA,

      referenceFacture: charge.referenceFacture ?? '',
      cheminJustificatif: charge.cheminJustificatif ?? '',
      observations: charge.observations ?? '',

      statutDeductibilite:
        Number(
          charge.statutDeductibilite ??
          StatutDeductibilite.AQualifier
        ),

      montantDeductibleFiscalement:
        charge.montantDeductibleFiscalement ?? null,

      justificationNonDeductibilite:
        charge.justificationNonDeductibilite ?? ''
    });

    this.appliquerValidationFiscale();

    this.chargerUnites(
      charge.bienImmobilierId,
      charge.uniteLocativeId
    );

    this.displayModal = true;
  }

  // ============================================================
  // UNITÉS LOCATIVES
  // ============================================================

  onBienChange(bienId: string | null): void {
    this.chargeForm.patchValue({
      uniteLocativeId: null
    });

    this.unitesOptions = [];

    if (bienId) {
      this.chargerUnites(bienId);
    }
  }

  private chargerUnites(
    bienId: string,
    uniteSelectionneeId: string | null = null
  ): void {
    this.biensService.getBienById(bienId).subscribe({
      next: (bien: any) => {
        const unites: UniteOption[] =
          bien?.unitesLocatives ?? [];

        this.unitesOptions = unites.map(unite => ({
          label: unite.reference || unite.nom || 'Unité locative',
          value: unite.id
        }));

        if (
          uniteSelectionneeId &&
          this.unitesOptions.some(
            option => option.value === uniteSelectionneeId
          )
        ) {
          this.chargeForm.patchValue({
            uniteLocativeId: uniteSelectionneeId
          });
        }
      },
      error: () => {
        this.unitesOptions = [];

        this.messageService.add({
          severity: 'warn',
          summary: 'Unités locatives',
          detail: 'Impossible de charger les unités de ce bien.'
        });
      }
    });
  }

  // ============================================================
  // ENREGISTREMENT
  // ============================================================

  enregistrer(): void {
    if (this.chargeForm.invalid || this.enregistrement) {
      this.chargeForm.markAllAsTouched();
      return;
    }

    const valeur = this.chargeForm.getRawValue();

    const requete: CreerChargeBienRequest = {
      bienImmobilierId: valeur.bienImmobilierId,
      uniteLocativeId: valeur.uniteLocativeId || null,

      dateDepense: valeur.dateDepense,
      dateFacture: valeur.dateFacture || null,
      datePaiement: valeur.datePaiement || null,

      typeCharge: Number(valeur.typeCharge) as TypeCharge,
      description: String(valeur.description).trim(),

      nomFournisseur:
        valeur.nomFournisseur?.trim() || null,

      numeroContribuableFournisseur:
        valeur.numeroContribuableFournisseur?.trim() || null,

      montantTTC: Number(valeur.montantTTC),
      montantHT: this.nombreOuNull(valeur.montantHT),
      montantTVA: this.nombreOuNull(valeur.montantTVA),

      referenceFacture:
        valeur.referenceFacture?.trim() || null,

      cheminJustificatif:
        valeur.cheminJustificatif?.trim() || null,

      observations:
        valeur.observations?.trim() || null
    };

    // Vérification de la cohérence HT + TVA.

    if (
      requete.montantHT !== null &&
      requete.montantTVA !== null &&
      requete.montantHT + requete.montantTVA >
        requete.montantTTC + 0.01
    ) {
      this.messageService.add({
        severity: 'error',
        summary: 'Montants invalides',
        detail:
          'Le montant HT additionné à la TVA ne peut pas dépasser le montant TTC.'
      });

      return;
    }

    // Vérification des dates.

    if (
      requete.dateFacture &&
      requete.dateFacture > requete.dateDepense
    ) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Dates incohérentes',
        detail:
          'La date de facture ne peut pas être postérieure à la date de dépense.'
      });

      return;
    }

    if (
      requete.datePaiement &&
      requete.datePaiement < requete.dateDepense
    ) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Dates incohérentes',
        detail:
          'La date de paiement ne peut pas précéder la date de dépense.'
      });

      return;
    }

    this.enregistrement = true;

    // MODIFICATION

    if (this.isEditMode && this.selectedChargeId) {
      const modification: ModifierChargeBienRequest = requete;

      const qualification = {
        statutDeductibilite:
          Number(valeur.statutDeductibilite) as StatutDeductibilite,

        montantDeductibleFiscalement:
          this.nombreOuNull(valeur.montantDeductibleFiscalement),

        justificationNonDeductibilite:
          valeur.justificationNonDeductibilite?.trim() || null
      };

      this.chargesService
        .updateCharge(this.selectedChargeId, modification)
        .pipe(
          switchMap(() =>
            this.chargesService.qualifierCharge(
              this.selectedChargeId!,
              qualification
            )
          ),
          finalize(() => {
            this.enregistrement = false;
          })
        )
        .subscribe({
          next: () => {
            this.displayModal = false;

            this.messageService.add({
              severity: 'success',
              summary: 'Modification réussie',
              detail:
                'La dépense et sa qualification fiscale ont été enregistrées.'
            });

            this.chargerCharges();
          },
          error: erreur => {
            this.messageService.add({
              severity: 'error',
              summary: 'Erreur de modification',
              detail: this.extraireMessageErreur(
                erreur,
                'Impossible de modifier la dépense ou sa qualification fiscale.'
              )
            });
          }
        });

      return;
    }

    // CRÉATION

    this.chargesService
      .createCharge(requete)
      .pipe(
        finalize(() => {
          this.enregistrement = false;
        })
      )
      .subscribe({
        next: () => {
          this.displayModal = false;

          this.messageService.add({
            severity: 'success',
            summary: 'Création réussie',
            detail:
              'La charge a été enregistrée et reste à qualifier fiscalement.'
          });

          this.chargerCharges();
        },
        error: erreur => {
          this.messageService.add({
            severity: 'error',
            summary: 'Erreur de création',
            detail: this.extraireMessageErreur(
              erreur,
              'Impossible d’enregistrer cette charge.'
            )
          });
        }
      });
  }

  // ============================================================
  // SUPPRESSION
  // ============================================================

  confirmerSuppression(charge: ChargeBienDto): void {
    this.confirmationService.confirm({
      header: 'Supprimer la charge',
      message: `Voulez-vous vraiment supprimer cette charge de ${this.formaterMontant(charge.montantTTC)} liée à « ${charge.nomBienImmobilier} » ?`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Supprimer',
      rejectLabel: 'Annuler',
      acceptButtonStyleClass: 'p-button-danger',
      rejectButtonStyleClass: 'p-button-text',
      accept: () => this.supprimerCharge(charge)
    });
  }

  private supprimerCharge(charge: ChargeBienDto): void {
    if (this.suppressionEnCours) {
      return;
    }

    this.suppressionEnCours = true;

    this.chargesService
      .deleteCharge(charge.id)
      .pipe(
        finalize(() => {
          this.suppressionEnCours = false;
        })
      )
      .subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Suppression réussie',
            detail: 'La charge a été supprimée.'
          });

          this.chargerCharges();
        },
        error: erreur => {
          this.messageService.add({
            severity: 'error',
            summary: 'Erreur de suppression',
            detail: this.extraireMessageErreur(
              erreur,
              'Impossible de supprimer cette charge.'
            )
          });
        }
      });
  }

  // ============================================================
  // LIBELLÉS ET STATUTS
  // ============================================================

  getTypeChargeLibelle(type: TypeCharge): string {
    return this.chargesService.getTypeChargeLibelle(type);
  }

  getTypeChargeSeverity(
    type: TypeCharge
  ): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (type) {
      case TypeCharge.TaxeFonciere:
        return 'warn';

      case TypeCharge.Entretien:
        return 'info';

      case TypeCharge.Assurance:
        return 'success';

      case TypeCharge.InteretEmprunt:
        return 'secondary';

      default:
        return 'info';
    }
  }

  getStatutDeductibiliteLibelle(
    statut: StatutDeductibilite
  ): string {
    return this.chargesService.getStatutDeductibiliteLibelle(statut);
  }

  getStatutDeductibiliteSeverity(
    statut: StatutDeductibilite
  ): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (statut) {
      case StatutDeductibilite.Deductible:
        return 'success';

      case StatutDeductibilite.NonDeductible:
        return 'danger';

      case StatutDeductibilite.PartiellementDeductible:
        return 'warn';

      case StatutDeductibilite.AQualifier:
      default:
        return 'secondary';
    }
  }

  // ============================================================
  // FORMATAGE
  // ============================================================

  formaterMontant(
    montant: number | null | undefined
  ): string {
    return new Intl.NumberFormat('fr-FR', {
      style: 'currency',
      currency: 'XAF',
      maximumFractionDigits: 0
    }).format(Number(montant ?? 0));
  }

  formaterDate(
    date: string | Date | null | undefined
  ): string {
    if (!date) {
      return '—';
    }

    const valeurDate = this.formatDateInput(date);

    if (!valeurDate) {
      return '—';
    }

    const [annee, mois, jour] = valeurDate.split('-');

    return `${jour}/${mois}/${annee}`;
  }

  private formatDateInput(
    date: string | Date | null | undefined
  ): string {
    if (!date) {
      return '';
    }

    if (typeof date === 'string') {
      const correspondance = date.match(/^(\d{4}-\d{2}-\d{2})/);

      if (correspondance) {
        return correspondance[1];
      }
    }

    const valeur = new Date(date);

    if (Number.isNaN(valeur.getTime())) {
      return '';
    }

    const annee = valeur.getFullYear();
    const mois = String(valeur.getMonth() + 1).padStart(2, '0');
    const jour = String(valeur.getDate()).padStart(2, '0');

    return `${annee}-${mois}-${jour}`;
  }

  // ============================================================
  // VALIDATION ET UTILITAIRES
  // ============================================================

  champInvalide(nom: string): boolean {
    const champ = this.chargeForm.get(nom);

    return !!champ &&
      champ.invalid &&
      (champ.touched || champ.dirty);
  }

  fermerModal(): void {
    if (!this.enregistrement) {
      this.displayModal = false;
    }
  }

  private nombreOuNull(valeur: unknown): number | null {
    if (
      valeur === null ||
      valeur === undefined ||
      valeur === ''
    ) {
      return null;
    }

    const nombre = Number(valeur);

    return Number.isFinite(nombre) ? nombre : null;
  }

  private extraireMessageErreur(
    erreur: any,
    messageParDefaut: string
  ): string {
    return (
      erreur?.error?.message ||
      erreur?.error?.title ||
      erreur?.error?.detail ||
      messageParDefaut
    );
  }
}
