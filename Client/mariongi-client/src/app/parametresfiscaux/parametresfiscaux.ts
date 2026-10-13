import { Component, OnInit, inject } from '@angular/core';

import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
  FormsModule
} from '@angular/forms';

import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';

// PrimeNG
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

import {
  MessageService,
  ConfirmationService
} from 'primeng/api';

import {
  ParametresFiscauxService
} from '../services/parametres-fiscaux.service';

import type {
  ParametreFiscalDto,
  CreerParametreFiscalRequest,
  ModifierParametreFiscalRequest
} from '../models/gestimmo.models';

import { AuthService } from '../login/auth.service';

interface DropdownItem {
  label: string;
  value: number;
}

interface TypeParametreOption {
  label: string;
  value: number;
}

@Component({
  selector: 'app-parametres-fiscaux',
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
    ConfirmDialogModule,
    TextareaModule,
    DatePickerModule,
    InputNumberModule
  ],

  providers: [
    MessageService,
    ConfirmationService
  ],

  templateUrl: './parametresfiscaux.html',
  styleUrl: './parametresfiscaux.scss'
})
export class ParametresFiscaux implements OnInit {

  // ============================================================
  // DÉPENDANCES
  // ============================================================

  private readonly fb = inject(FormBuilder);

  private readonly parametresFiscauxService =
    inject(ParametresFiscauxService);

  private readonly messageService =
    inject(MessageService);

  private readonly confirmationService =
    inject(ConfirmationService);

  private readonly authService =
    inject(AuthService);

  // ============================================================
  // DONNÉES
  // ============================================================

  parametresFiscaux: ParametreFiscalDto[] = [];

  loading = false;
  saving = false;

  recherche = '';

  filtreAnnee: number | null = null;
  filtreType: number | null = null;

  anneesOptions: DropdownItem[] = [];

  // ============================================================
  // PERMISSIONS
  // ============================================================

  canAccessParametresFiscaux = false;
  canManageParametresFiscaux = false;

  // ============================================================
  // DIALOGUE ET FORMULAIRE
  // ============================================================

  parametreFiscalDialog = false;

  parametreFiscalForm!: FormGroup;

  isEditMode = false;

  selectedParametreFiscalId?: string;

  // ============================================================
  // TYPES DE PARAMÈTRES
  // IMPORTANT : adapter les valeurs à l'énumération C# réelle.
  // ============================================================

typeOptions: TypeParametreOption[] = [
  { label: 'Taux', value: 1 },
  { label: 'Montant fixe', value: 2 },
  { label: 'Pourcentage forfaitaire', value: 3 },
  { label: 'Seuil', value: 4 }
];

  // ============================================================
  // INITIALISATION
  // ============================================================

  ngOnInit(): void {
    this.verifierPermissions();
    this.initForm();

    if (this.canAccessParametresFiscaux) {
      this.chargerParametresFiscaux();
    }
  }

  // ============================================================
  // PERMISSIONS
  // ============================================================

  private verifierPermissions(): void {
    this.canAccessParametresFiscaux =
      this.parametresFiscauxService
        .canAccessParametresFiscaux();

    this.canManageParametresFiscaux =
      this.parametresFiscauxService
        .canManageParametresFiscaux();
  }

  // ============================================================
  // FORMULAIRE
  // ============================================================

  private initForm(): void {
    this.parametreFiscalForm = this.fb.group({
      anneeFiscale: [
        new Date().getFullYear(),
        [
          Validators.required,
          Validators.min(2000),
          Validators.max(2100)
        ]
      ],

      codeRegle: [
        '',
        [
          Validators.required,
          Validators.maxLength(100)
        ]
      ],

      description: [
        '',
        [
          Validators.required,
          Validators.maxLength(1000)
        ]
      ],

      valeur: [
        0,
        [
          Validators.required,
          Validators.min(0)
        ]
      ],

      typeParametre: [
        null,
        Validators.required
      ],

      dateDebutValidite: [
        new Date(),
        Validators.required
      ],

      dateFinValidite: [
        null
      ]
    });
  }

  // ============================================================
  // CHARGEMENT
  // ============================================================

  chargerParametresFiscaux(): void {
    this.loading = true;

    this.parametresFiscauxService
      .getParametresFiscaux()
      .subscribe({
        next: (data: ParametreFiscalDto[]) => {
          this.parametresFiscaux =
            Array.isArray(data) ? [...data] : [];

          this.construireAnneesOptions();

          this.loading = false;
        },

        error: (err: HttpErrorResponse) => {
          this.loading = false;

          this.traiterErreurHttp(err, 'chargement');
        }
      });
  }

  private construireAnneesOptions(): void {
    const annees = new Set<number>();

    annees.add(new Date().getFullYear());

    for (const parametre of this.parametresFiscaux) {
      annees.add(parametre.anneeFiscale);
    }

    this.anneesOptions = [...annees]
      .sort((a, b) => b - a)
      .map(annee => ({
        label: String(annee),
        value: annee
      }));
  }

  // ============================================================
  // RECHERCHE ET FILTRES
  // ============================================================

  get parametresFiltres(): ParametreFiscalDto[] {
    const terme = this.recherche.trim().toLowerCase();

    return this.parametresFiscaux.filter(parametre => {
      const correspondRecherche =
        !terme ||
        parametre.codeRegle?.toLowerCase().includes(terme) ||
        parametre.description?.toLowerCase().includes(terme);

      const correspondAnnee =
        this.filtreAnnee === null ||
        parametre.anneeFiscale === this.filtreAnnee;

      const correspondType =
        this.filtreType === null ||
        parametre.typeParametre === this.filtreType;

      return correspondRecherche &&
        correspondAnnee &&
        correspondType;
    });
  }

  reinitialiserFiltres(): void {
    this.recherche = '';
    this.filtreAnnee = null;
    this.filtreType = null;
  }

  // ============================================================
  // AFFICHAGE
  // ============================================================

  getTypeLabel(type: number): string {
    return this.typeOptions.find(
      option => option.value === type
    )?.label ?? 'Type inconnu';
  }

  getTypeSeverity(
    type: number
  ): 'info' | 'success' | 'warn' | 'secondary' {
    switch (type) {
      case 0:
        return 'info';
      case 1:
        return 'success';
      case 2:
        return 'warn';
      default:
        return 'secondary';
    }
  }

  formaterValeur(parametre: ParametreFiscalDto): string {
    if (parametre.typeParametre === 0) {
      return `${(parametre.valeur * 100).toLocaleString('fr-FR')} %`;
    }

    return parametre.valeur.toLocaleString('fr-FR', {
      maximumFractionDigits: 4
    });
  }

  // ============================================================
  // CRÉATION
  // ============================================================

  openNew(): void {
    if (!this.canManageParametresFiscaux) {
      this.afficherAccesRefuse();
      return;
    }

    this.isEditMode = false;
    this.selectedParametreFiscalId = undefined;

    this.parametreFiscalForm.reset({
      anneeFiscale: new Date().getFullYear(),
      codeRegle: '',
      description: '',
      valeur: 0,
      typeParametre: null,
      dateDebutValidite: new Date(),
      dateFinValidite: null
    });

    this.parametreFiscalDialog = true;
  }

  // ============================================================
  // MODIFICATION
  // ============================================================

  editParametreFiscal(
    parametre: ParametreFiscalDto
  ): void {
    if (!this.canManageParametresFiscaux) {
      this.afficherAccesRefuse();
      return;
    }

    if (!parametre.id) {
      this.messageService.add({
        severity: 'error',
        summary: 'Erreur',
        detail: 'L’identifiant du paramètre fiscal est introuvable.'
      });

      return;
    }

    this.isEditMode = true;
    this.selectedParametreFiscalId = parametre.id;

    this.parametreFiscalForm.reset({
      anneeFiscale: parametre.anneeFiscale,
      codeRegle: parametre.codeRegle ?? '',
      description: parametre.description ?? '',
      valeur: parametre.valeur,
      typeParametre: parametre.typeParametre,
      dateDebutValidite:
        this.convertirDate(parametre.dateDebutValidite),
      dateFinValidite:
        parametre.dateFinValidite
          ? this.convertirDate(parametre.dateFinValidite)
          : null
    });

    this.parametreFiscalDialog = true;
  }

  // ============================================================
  // SAUVEGARDE
  // ============================================================

  saveParametreFiscal(): void {
    if (!this.canManageParametresFiscaux) {
      this.afficherAccesRefuse();
      return;
    }

    if (this.saving) {
      return;
    }

    if (this.parametreFiscalForm.invalid) {
      this.parametreFiscalForm.markAllAsTouched();
      return;
    }

    const value = this.parametreFiscalForm.getRawValue();

    const dateDebut = this.convertirDate(
      value.dateDebutValidite
    );

    const dateFin = value.dateFinValidite
      ? this.convertirDate(value.dateFinValidite)
      : null;

    if (
      !dateDebut ||
      (dateFin && dateFin.getTime() < dateDebut.getTime())
    ) {
      this.messageService.add({
        severity: 'error',
        summary: 'Dates invalides',
        detail:
          'La date de fin doit être supérieure ou égale à la date de début.'
      });

      return;
    }

    const payload: CreerParametreFiscalRequest = {
      anneeFiscale: Number(value.anneeFiscale),
      codeRegle: String(value.codeRegle ?? '').trim().toUpperCase(),
      description: String(value.description ?? '').trim(),
      valeur: Number(value.valeur),
      typeParametre: Number(value.typeParametre),
      dateDebutValidite: dateDebut.toISOString(),
      dateFinValidite: dateFin
        ? dateFin.toISOString()
        : null
    };

    if (
      !payload.codeRegle ||
      !payload.description
    ) {
      this.parametreFiscalForm.markAllAsTouched();
      return;
    }

    this.saving = true;

    if (this.isEditMode) {
      if (!this.selectedParametreFiscalId) {
        this.saving = false;

        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: 'L’identifiant du paramètre fiscal est manquant.'
        });

        return;
      }

      const request: ModifierParametreFiscalRequest = payload;

      this.parametresFiscauxService
        .updateParametreFiscal(
          this.selectedParametreFiscalId,
          request
        )
        .subscribe({
          next: () => {
            this.saving = false;

            this.messageService.add({
              severity: 'success',
              summary: 'Succès',
              detail: 'Paramètre fiscal modifié avec succès.'
            });

            this.fermerDialog();
            this.chargerParametresFiscaux();
          },

          error: (err: HttpErrorResponse) => {
            this.saving = false;
            this.traiterErreurHttp(err, 'modification');
          }
        });

      return;
    }

    this.parametresFiscauxService
      .createParametreFiscal(payload)
      .subscribe({
        next: () => {
          this.saving = false;

          this.messageService.add({
            severity: 'success',
            summary: 'Succès',
            detail: 'Paramètre fiscal créé avec succès.'
          });

          this.fermerDialog();
          this.chargerParametresFiscaux();
        },

        error: (err: HttpErrorResponse) => {
          this.saving = false;
          this.traiterErreurHttp(err, 'création');
        }
      });
  }

  // ============================================================
  // FERMETURE DU DIALOGUE
  // ============================================================

  fermerDialog(): void {
    this.parametreFiscalDialog = false;
    this.isEditMode = false;
    this.selectedParametreFiscalId = undefined;

    this.parametreFiscalForm.reset({
      anneeFiscale: new Date().getFullYear(),
      codeRegle: '',
      description: '',
      valeur: 0,
      typeParametre: null,
      dateDebutValidite: new Date(),
      dateFinValidite: null
    });
  }

  // ============================================================
  // SUPPRESSION LOGIQUE
  // ============================================================

  deleteParametreFiscal(
    parametre: ParametreFiscalDto
  ): void {
    if (!this.canManageParametresFiscaux) {
      this.afficherAccesRefuse();
      return;
    }

    if (!parametre.id) {
      return;
    }

    this.confirmationService.confirm({
      message:
        `Voulez-vous vraiment supprimer le paramètre « ${parametre.codeRegle} » ?`,

      header: 'Confirmation de suppression',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Oui, supprimer',
      rejectLabel: 'Annuler',
      acceptButtonStyleClass: 'p-button-danger',

      accept: () => {
        this.parametresFiscauxService
          .deleteParametreFiscal(parametre.id)
          .subscribe({
            next: () => {
              this.messageService.add({
                severity: 'success',
                summary: 'Succès',
                detail: 'Paramètre fiscal supprimé avec succès.'
              });

              this.chargerParametresFiscaux();
            },

            error: (err: HttpErrorResponse) => {
              this.traiterErreurHttp(err, 'suppression');
            }
          });
      }
    });
  }

  // ============================================================
  // UTILITAIRES
  // ============================================================

  private convertirDate(value: string | Date): Date {
    return value instanceof Date
      ? value
      : new Date(value);
  }

  getShortId(id?: string): string {
    return id
      ? id.substring(0, 8).toUpperCase()
      : 'N/A';
  }

  private afficherAccesRefuse(): void {
    this.messageService.add({
      severity: 'warn',
      summary: 'Accès refusé',
      detail:
        'Vous ne disposez pas des droits nécessaires pour effectuer cette opération.'
    });
  }

  // ============================================================
  // ERREURS HTTP
  // ============================================================

  private traiterErreurHttp(
    err: HttpErrorResponse,
    action:
      | 'chargement'
      | 'création'
      | 'modification'
      | 'suppression'
  ): void {
    console.error(
      `Erreur lors du ${action} du paramètre fiscal :`,
      err
    );

    if (err.status === 401) {
      this.messageService.add({
        severity: 'error',
        summary: 'Non authentifié',
        detail:
          'Votre session a expiré. Veuillez vous reconnecter.'
      });

      return;
    }

    if (err.status === 403) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Accès refusé',
        detail: 'Vous ne disposez pas des droits nécessaires.'
      });

      return;
    }

    const apiMessage =
      err.error?.message ??
      err.error?.title;

    if (
      typeof apiMessage === 'string' &&
      apiMessage.trim()
    ) {
      this.messageService.add({
        severity: 'error',
        summary: 'Erreur',
        detail: apiMessage,
        life: 5000
      });

      return;
    }

    this.messageService.add({
      severity: 'error',
      summary: 'Erreur',
      detail:
        `Une erreur est survenue lors du ${action} du paramètre fiscal (code ${err.status || 'inconnu'}).`,
      life: 5000
    });
  }
}