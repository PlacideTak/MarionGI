
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';

// PrimeNG
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TextareaModule } from 'primeng/textarea';
import { DatePickerModule } from 'primeng/datepicker';
import { MessageService, ConfirmationService } from 'primeng/api';

import { ValeursFiscalesBiensService } from '../services/valeurs-fiscales-biens.service';
import { BiensService } from '../services/biens.service';

import {
  TypeValeurFiscale,
  ValeurFiscaleBienDto,
  CreerValeurFiscaleBienRequest,
  ModifierValeurFiscaleBienRequest
} from '../models/gestimmo.models';

interface Option<T> {
  label: string;
  value: T;
}

interface BienOption {
  label: string;
  value: string;
}

@Component({
  selector: 'app-valeursfiscalesbiens',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    ToastModule,
    ConfirmDialogModule,
    TextareaModule,
    DatePickerModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './valeursfiscalesbiens.html',
  styleUrl: './valeursfiscalesbiens.scss'
})
export class ValeursFiscalesBiens implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly valeursService = inject(ValeursFiscalesBiensService);
  private readonly biensService = inject(BiensService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  valeursFiscales: ValeurFiscaleBienDto[] = [];
  biensOptions: BienOption[] = [];

  loading = false;
  saving = false;
  recherche = '';

  filtreBien: string | null = null;
  filtreAnnee: number | null = null;
  filtreType: TypeValeurFiscale | null = null;

  dialogVisible = false;
  isEditMode = false;
  selectedId?: string;
  valeurForm!: FormGroup;

  readonly anneeActuelle = new Date().getFullYear();

  readonly typeOptions: Option<TypeValeurFiscale>[] = [
    { label: 'Estimée', value: TypeValeurFiscale.Estimee },
    { label: 'Déclarée', value: TypeValeurFiscale.Declaree },
    { label: 'Officielle', value: TypeValeurFiscale.Officielle },
    { label: 'Expertise', value: TypeValeurFiscale.Expertise }
  ];

  ngOnInit(): void {
    this.initForm();
    this.chargerBiens();
    this.chargerValeursFiscales();
  }

  private initForm(): void {
    this.valeurForm = this.fb.group({
      bienImmobilierId: ['', Validators.required],
      anneeFiscale: [
        this.anneeActuelle,
        [
          Validators.required,
          Validators.min(1900),
          Validators.max(this.anneeActuelle + 1)
        ]
      ],
      montant: [null, [Validators.required, Validators.min(0.01)]],
      typeValeur: [null, Validators.required],
      dateEvaluation: [new Date(), Validators.required],
      dateValidationOfficielle: [null],
      referenceJustificative: ['', Validators.maxLength(250)],
      cheminJustificatif: ['', Validators.maxLength(500)],
      observations: ['', Validators.maxLength(2000)]
    });
  }
  

  private chargerBiens(): void {
    this.biensService.getBiens().subscribe({
      next: (biens) => {
        this.biensOptions = Array.isArray(biens)
          ? biens.map(bien => ({
              label: `${bien.reference} - ${bien.nom}`,
              value: bien.id
            }))
          : [];
      },
      error: (err: HttpErrorResponse) => {
        this.traiterErreurHttp(err, 'chargement des biens');
      }
    });
  }

  chargerValeursFiscales(): void {
    this.loading = true;

    this.valeursService.getAll().subscribe({
      next: (data) => {
        this.valeursFiscales = Array.isArray(data) ? data : [];
        this.loading = false;
      },
      error: (err: HttpErrorResponse) => {
        this.loading = false;
        this.traiterErreurHttp(err, 'chargement');
      }
    });
  }

  get anneesOptions(): Option<number>[] {
    const annees = new Set<number>([this.anneeActuelle]);

    this.valeursFiscales.forEach(v => annees.add(v.anneeFiscale));

    return [...annees]
      .sort((a, b) => b - a)
      .map(annee => ({ label: String(annee), value: annee }));
  }

  get valeursFiltrees(): ValeurFiscaleBienDto[] {
    const terme = this.recherche.trim().toLowerCase();

    return this.valeursFiscales.filter(v => {
      const rechercheOK =
        !terme ||
        v.referenceBien?.toLowerCase().includes(terme) ||
        v.nomBien?.toLowerCase().includes(terme) ||
        v.referenceJustificative?.toLowerCase().includes(terme) ||
        v.observations?.toLowerCase().includes(terme);

      const bienOK = !this.filtreBien ||
        v.bienImmobilierId === this.filtreBien;

      const anneeOK = this.filtreAnnee === null ||
        v.anneeFiscale === this.filtreAnnee;

      const typeOK = this.filtreType === null ||
        v.typeValeur === this.filtreType;

      return rechercheOK && bienOK && anneeOK && typeOK;
    });
  }

  reinitialiserFiltres(): void {
    this.recherche = '';
    this.filtreBien = null;
    this.filtreAnnee = null;
    this.filtreType = null;
  }

  getTypeLabel(type: TypeValeurFiscale): string {
    return this.typeOptions.find(x => x.value === type)?.label
      ?? 'Type inconnu';
  }

  getTypeSeverity(
    type: TypeValeurFiscale
  ): 'info' | 'success' | 'warn' | 'secondary' {
    switch (type) {
      case TypeValeurFiscale.Estimee:
        return 'info';
      case TypeValeurFiscale.Declaree:
        return 'secondary';
      case TypeValeurFiscale.Officielle:
        return 'success';
      case TypeValeurFiscale.Expertise:
        return 'warn';
      default:
        return 'secondary';
    }
  }

  get nombreAnnees(): number {
    return new Set(this.valeursFiscales.map(v => v.anneeFiscale)).size;
  }

  get montantTotal(): number {
    return this.valeursFiltrees.reduce((total, v) => total + v.montant, 0);
  }

  openNew(): void {
    this.isEditMode = false;
    this.selectedId = undefined;

    this.valeurForm.reset({
      bienImmobilierId: this.filtreBien ?? '',
      anneeFiscale: this.anneeActuelle,
      montant: null,
      typeValeur: null,
      dateEvaluation: new Date(),
      dateValidationOfficielle: null,
      referenceJustificative: '',
      cheminJustificatif: '',
      observations: ''
    });

    this.dialogVisible = true;
  }

  editValeur(valeur: ValeurFiscaleBienDto): void {
    if (!valeur.id) {
      this.messageService.add({
        severity: 'error',
        summary: 'Erreur',
        detail: 'Identifiant de la valeur fiscale introuvable.'
      });
      return;
    }

    this.isEditMode = true;
    this.selectedId = valeur.id;

    this.valeurForm.reset({
      bienImmobilierId: valeur.bienImmobilierId,
      anneeFiscale: valeur.anneeFiscale,
      montant: valeur.montant,
      typeValeur: valeur.typeValeur,
      dateEvaluation: this.convertirDate(valeur.dateEvaluation),
      dateValidationOfficielle: valeur.dateValidationOfficielle
        ? this.convertirDate(valeur.dateValidationOfficielle)
        : null,
      referenceJustificative: valeur.referenceJustificative ?? '',
      cheminJustificatif: valeur.cheminJustificatif ?? '',
      observations: valeur.observations ?? ''
    });

    this.dialogVisible = true;
  }

  saveValeur(): void {
    if (this.saving) return;

    if (this.valeurForm.invalid) {
      this.valeurForm.markAllAsTouched();
      return;
    }

    const v = this.valeurForm.getRawValue();
    const dateEvaluation = this.convertirDate(v.dateEvaluation);
    const dateValidation = v.dateValidationOfficielle
      ? this.convertirDate(v.dateValidationOfficielle)
      : null;

    if (
      !dateEvaluation ||
      dateEvaluation.getTime() > new Date().setHours(23, 59, 59, 999)
    ) {
      this.messageService.add({
        severity: 'error',
        summary: 'Date invalide',
        detail: 'La date d’évaluation ne peut pas être dans le futur.'
      });
      return;
    }

    if (
      dateValidation &&
      dateValidation.getTime() < dateEvaluation.getTime()
    ) {
      this.messageService.add({
        severity: 'error',
        summary: 'Dates invalides',
        detail: 'La validation officielle ne peut pas précéder l’évaluation.'
      });
      return;
    }

    if (dateValidation && dateValidation.getTime() > new Date().setHours(23, 59, 59, 999)) {
      this.messageService.add({
        severity: 'error',
        summary: 'Date invalide',
        detail: 'La date de validation officielle ne peut pas être dans le futur.'
      });
      return;
    }

    const payload: CreerValeurFiscaleBienRequest = {
      bienImmobilierId: String(v.bienImmobilierId),
      anneeFiscale: Number(v.anneeFiscale),
      montant: Number(v.montant),
      typeValeur: Number(v.typeValeur) as TypeValeurFiscale,
      dateEvaluation: dateEvaluation.toISOString(),
      dateValidationOfficielle: dateValidation?.toISOString() ?? null,
      referenceJustificative: this.nettoyer(v.referenceJustificative),
      cheminJustificatif: this.nettoyer(v.cheminJustificatif),
      observations: this.nettoyer(v.observations)
    };

    this.saving = true;

    if (this.isEditMode) {
      if (!this.selectedId) {
        this.saving = false;
        return;
      }

      const request: ModifierValeurFiscaleBienRequest = payload;

      this.valeursService.update(this.selectedId, request).subscribe({
        next: () => this.succesSauvegarde('modifiée'),
        error: (err: HttpErrorResponse) => {
          this.saving = false;
          this.traiterErreurHttp(err, 'modification');
        }
      });
    } else {
      this.valeursService.create(payload).subscribe({
        next: () => this.succesSauvegarde('créée'),
        error: (err: HttpErrorResponse) => {
          this.saving = false;
          this.traiterErreurHttp(err, 'création');
        }
      });
    }
  }

  private succesSauvegarde(action: string): void {
    this.saving = false;

    this.messageService.add({
      severity: 'success',
      summary: 'Succès',
      detail: `Valeur fiscale ${action} avec succès.`
    });

    this.fermerDialog();
    this.chargerValeursFiscales();
  }

  deleteValeur(valeur: ValeurFiscaleBienDto): void {
    if (!valeur.id) return;

    this.confirmationService.confirm({
      message: `Supprimer la valeur fiscale du bien « ${valeur.nomBien} » pour ${valeur.anneeFiscale} ?`,
      header: 'Confirmation de suppression',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Oui, supprimer',
      rejectLabel: 'Annuler',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.valeursService.delete(valeur.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Succès',
              detail: 'Valeur fiscale supprimée avec succès.'
            });
            this.chargerValeursFiscales();
          },
          error: (err: HttpErrorResponse) =>
            this.traiterErreurHttp(err, 'suppression')
        });
      }
    });
  }

  fermerDialog(): void {
    if (this.saving) return;

    this.dialogVisible = false;
    this.isEditMode = false;
    this.selectedId = undefined;
  }

  private convertirDate(value: string | Date): Date {
    return value instanceof Date ? value : new Date(value);
  }

  private nettoyer(value: unknown): string | null {
    const texte = String(value ?? '').trim();
    return texte || null;
  }

  private traiterErreurHttp(
    err: HttpErrorResponse,
    action: string
  ): void {
    console.error(`Erreur lors du ${action} d’une valeur fiscale :`, err);

    let detail = `Une erreur est survenue lors du ${action} (code ${err.status || 'inconnu'}).`;

    if (err.status === 401) {
      detail = 'Votre session a expiré. Veuillez vous reconnecter.';
    } else if (err.status === 403) {
      detail = 'Vous ne disposez pas des droits nécessaires.';
    } else if (typeof err.error?.message === 'string' && err.error.message.trim()) {
      detail = err.error.message;
    } else if (typeof err.error?.title === 'string' && err.error.title.trim()) {
      detail = err.error.title;
    }

    this.messageService.add({
      severity: err.status === 403 ? 'warn' : 'error',
      summary: err.status === 401 ? 'Non authentifié' : 'Erreur',
      detail,
      life: 5000
    });
  }
}
