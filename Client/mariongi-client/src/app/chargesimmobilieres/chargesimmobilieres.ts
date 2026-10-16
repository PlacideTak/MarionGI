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
import { finalize } from 'rxjs';

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
  TypeCharge
} from  '../models/gestimmo.models';

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

  charges: ChargeBienDto[] = [];
  chargesFiltrees: ChargeBienDto[] = [];

  biensOptions: OptionSelect[] = [];
  unitesOptions: OptionSelect[] = [];
  typesCharges = this.chargesService.typesCharges;

  chargement = false;
  chargementBiens = false;
  enregistrement = false;

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
    typeCharge: [TypeCharge.Entretien, Validators.required],
    description: ['', [Validators.required, Validators.maxLength(2000)]],
    montantTTC: [null, [Validators.required, Validators.min(0.01)]],
    montantHT: [null, Validators.min(0)],
    montantTVA: [null, Validators.min(0)],
    referenceFacture: ['', Validators.maxLength(100)],
    estDeductibleFiscalement: [true, Validators.required],
    justificationNonDeductibilite: ['', Validators.maxLength(1000)]
  });

  ngOnInit(): void {
    this.chargerBiens();
    this.chargerCharges();
    this.gererValidationDeductibilite();
  }

  get totalCharges(): number {
    return this.chargesFiltrees.length;
  }

  get montantTotalTTC(): number {
    return this.chargesFiltrees.reduce(
      (total, charge) => total + Number(charge.montantTTC || 0),
      0
    );
  }

  get montantTotalDeductible(): number {
    return this.chargesFiltrees
      .filter(charge => charge.estDeductibleFiscalement)
      .reduce(
        (total, charge) => total + Number(charge.montantTTC || 0),
        0
      );
  }

  get nombreBiensConcernes(): number {
    return new Set(
      this.chargesFiltrees.map(charge => charge.bienImmobilierId)
    ).size;
  }

  get f(): { [key: string]: AbstractControl } {
    return this.chargeForm.controls;
  }

  get justificationRequise(): boolean {
    return this.chargeForm.get('estDeductibleFiscalement')?.value === false;
  }

  private chargerBiens(): void {
    this.chargementBiens = true;

    this.biensService.getBiens()
      .pipe(finalize(() => (this.chargementBiens = false)))
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

  chargerCharges(): void {
    this.chargement = true;

    this.chargesService.getCharges({
      bienImmobilierId: this.filtreBienId,
      uniteLocativeId: this.filtreUniteId,
      typeCharge: this.filtreTypeCharge,
      dateDebut: this.filtreDateDebut || null,
      dateFin: this.filtreDateFin || null
    })
      .pipe(finalize(() => (this.chargement = false)))
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
        this.getTypeChargeLibelle(charge.typeCharge)
      ]
        .filter(Boolean)
        .join(' ')
        .toLocaleLowerCase();

      return texte.includes(terme);
    });
  }

  ouvrirCreation(): void {
    this.isEditMode = false;
    this.selectedChargeId = null;
    this.unitesOptions = [];

    this.chargeForm.reset({
      bienImmobilierId: '',
      uniteLocativeId: null,
      dateDepense: this.aujourdHui,
      typeCharge: TypeCharge.Entretien,
      description: '',
      montantTTC: null,
      montantHT: null,
      montantTVA: null,
      referenceFacture: '',
      estDeductibleFiscalement: true,
      justificationNonDeductibilite: ''
    });

    this.chargeForm.markAsPristine();
    this.chargeForm.markAsUntouched();
    this.displayModal = true;
  }

  ouvrirModification(charge: ChargeBienDto): void {
    this.isEditMode = true;
    this.selectedChargeId = charge.id;

    this.chargeForm.reset({
      bienImmobilierId: charge.bienImmobilierId,
      uniteLocativeId: charge.uniteLocativeId,
      dateDepense: this.formatDateInput(charge.dateDepense),
      typeCharge: charge.typeCharge,
      description: charge.description,
      montantTTC: charge.montantTTC,
      montantHT: charge.montantHT,
      montantTVA: charge.montantTVA,
      referenceFacture: charge.referenceFacture ?? '',
      estDeductibleFiscalement: charge.estDeductibleFiscalement,
      justificationNonDeductibilite:
        charge.justificationNonDeductibilite ?? ''
    });

    this.chargerUnites(charge.bienImmobilierId, charge.uniteLocativeId);
    this.displayModal = true;
  }

  onBienChange(bienId: string | null): void {
    this.chargeForm.patchValue({ uniteLocativeId: null });
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
        const unites: UniteOption[] = bien?.unitesLocatives ?? [];

        this.unitesOptions = unites.map(unite => ({
          label: unite.reference || unite.nom || 'Unité locative',
          value: unite.id
        }));

        if (
          uniteSelectionneeId &&
          this.unitesOptions.some(option => option.value === uniteSelectionneeId)
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

  private gererValidationDeductibilite(): void {
    const justification = this.chargeForm.get(
      'justificationNonDeductibilite'
    );

    this.chargeForm
      .get('estDeductibleFiscalement')
      ?.valueChanges.subscribe(estDeductible => {
        if (estDeductible) {
          justification?.clearValidators();
          justification?.setValue('');
        } else {
          justification?.setValidators([
            Validators.required,
            Validators.maxLength(1000)
          ]);
        }

        justification?.updateValueAndValidity({ emitEvent: false });
      });
  }

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
      typeCharge: Number(valeur.typeCharge) as TypeCharge,
      description: String(valeur.description).trim(),
      montantTTC: Number(valeur.montantTTC),
      montantHT: this.nombreOuNull(valeur.montantHT),
      montantTVA: this.nombreOuNull(valeur.montantTVA),
      referenceFacture: valeur.referenceFacture?.trim() || null,
      estDeductibleFiscalement: Boolean(valeur.estDeductibleFiscalement),
      justificationNonDeductibilite:
        valeur.estDeductibleFiscalement
          ? null
          : valeur.justificationNonDeductibilite?.trim() || null
    };

    if (
      requete.montantHT !== null &&
      requete.montantTVA !== null &&
      requete.montantHT + requete.montantTVA > requete.montantTTC + 0.01
    ) {
      this.messageService.add({
        severity: 'error',
        summary: 'Montants invalides',
        detail: 'Le montant HT additionné à la TVA ne peut pas dépasser le montant TTC.'
      });
      return;
    }

    this.enregistrement = true;

    if (this.isEditMode && this.selectedChargeId) {
      const modification: ModifierChargeBienRequest = requete;

      this.chargesService
        .updateCharge(this.selectedChargeId, modification)
        .pipe(finalize(() => (this.enregistrement = false)))
        .subscribe({
          next: () => {
            this.displayModal = false;
            this.messageService.add({
              severity: 'success',
              summary: 'Modification réussie',
              detail: 'La charge a été modifiée.'
            });
            this.chargerCharges();
          },
          error: erreur => {
            this.messageService.add({
              severity: 'error',
              summary: 'Erreur de modification',
              detail: this.extraireMessageErreur(
                erreur,
                'Impossible de modifier cette charge.'
              )
            });
          }
        });

      return;
    }

    this.chargesService
      .createCharge(requete)
      .pipe(finalize(() => (this.enregistrement = false)))
      .subscribe({
        next: () => {
          this.displayModal = false;
          this.messageService.add({
            severity: 'success',
            summary: 'Création réussie',
            detail: 'La charge a été enregistrée.'
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
    this.chargesService.deleteCharge(charge.id).subscribe({
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

  formaterMontant(montant: number | null | undefined): string {
    return new Intl.NumberFormat('fr-FR', {
      style: 'currency',
      currency: 'XAF',
      maximumFractionDigits: 0
    }).format(Number(montant ?? 0));
  }

  formaterDate(date: string | Date | null | undefined): string {
    if (!date) {
      return '—';
    }

    const valeur = new Date(date);

    if (Number.isNaN(valeur.getTime())) {
      return '—';
    }

    return new Intl.DateTimeFormat('fr-FR').format(valeur);
  }

  champInvalide(nom: string): boolean {
    const champ = this.chargeForm.get(nom);
    return !!champ && champ.invalid && (champ.touched || champ.dirty);
  }

  fermerModal(): void {
    if (!this.enregistrement) {
      this.displayModal = false;
    }
  }

  private nombreOuNull(valeur: unknown): number | null {
    if (valeur === null || valeur === undefined || valeur === '') {
      return null;
    }

    const nombre = Number(valeur);
    return Number.isFinite(nombre) ? nombre : null;
  }

  private formatDateInput(date: string | Date): string {
    const valeur = new Date(date);

    if (Number.isNaN(valeur.getTime())) {
      return '';
    }

    const annee = valeur.getFullYear();
    const mois = String(valeur.getMonth() + 1).padStart(2, '0');
    const jour = String(valeur.getDate()).padStart(2, '0');

    return `${annee}-${mois}-${jour}`;
  }

  private extraireMessageErreur(erreur: any, messageParDefaut: string): string {
    return (
      erreur?.error?.message ||
      erreur?.error?.title ||
      erreur?.error?.detail ||
      messageParDefaut
    );
  }
}