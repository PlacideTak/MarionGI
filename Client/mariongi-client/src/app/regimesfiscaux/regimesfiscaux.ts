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

import {
  MessageService,
  ConfirmationService
} from 'primeng/api';

// Service : classe utilisée à l'exécution
import { RegimesFiscauxService } from '../services/regimes-fiscaux.service';

// Modèles : types utilisés uniquement par TypeScript
import type {
  RegimeFiscalDto,
  CreerRegimeFiscalRequest,
  ModifierRegimeFiscalRequest
} from '../models/gestimmo.models';
import { AuthService } from '../login/auth.service';


interface DropdownItem {
  label: string;
  value: boolean;
}


@Component({
  selector: 'app-regimes-fiscaux',
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
    TextareaModule
  ],

  providers: [
    MessageService,
    ConfirmationService
  ],

  templateUrl: './regimesfiscaux.html',
  styleUrl: './regimesfiscaux.scss'
})
export class RegimesFiscaux implements OnInit {

  // ============================================================
  // DÉPENDANCES
  // ============================================================

  private readonly fb = inject(FormBuilder);

  private readonly regimesFiscauxService =
    inject(RegimesFiscauxService);

  private readonly messageService =
    inject(MessageService);

  private readonly confirmationService =
    inject(ConfirmationService);

  private readonly authService =
    inject(AuthService);


  // ============================================================
  // DONNÉES
  // ============================================================

  regimesFiscaux: RegimeFiscalDto[] = [];

  loading = false;

  recherche = '';

  filtreActif: boolean | null = null;


  // ============================================================
  // PERMISSIONS
  // ============================================================

  canAccessRegimesFiscaux = false;

  canManageRegimesFiscaux = false;


  // ============================================================
  // DIALOGUE ET FORMULAIRE
  // ============================================================

  regimeFiscalDialog = false;

  regimeFiscalForm!: FormGroup;

  isEditMode = false;

  selectedRegimeFiscalId?: string;

  saving = false;


  // ============================================================
  // OPTIONS
  // ============================================================

  statutOptions: DropdownItem[] = [
    {
      label: 'Actif',
      value: true
    },
    {
      label: 'Inactif',
      value: false
    }
  ];


  // ============================================================
  // INITIALISATION
  // ============================================================

  ngOnInit(): void {

    this.verifierPermissions();

    this.initForm();

    if (this.canAccessRegimesFiscaux) {
      this.chargerRegimesFiscaux();
    }
  }


  // ============================================================
  // PERMISSIONS
  // ============================================================

  private verifierPermissions(): void {

    this.canAccessRegimesFiscaux =
      this.regimesFiscauxService
        .canAccessRegimesFiscaux();

    this.canManageRegimesFiscaux =
      this.regimesFiscauxService
        .canManageRegimesFiscaux();
  }


  // ============================================================
  // FORMULAIRE
  // ============================================================

  private initForm(): void {

    this.regimeFiscalForm = this.fb.group({

      code: [
        '',
        [
          Validators.required,
          Validators.maxLength(50)
        ]
      ],

      libelle: [
        '',
        [
          Validators.required,
          Validators.maxLength(150)
        ]
      ],

      description: [
        '',
        [
          Validators.maxLength(1000)
        ]
      ],

      estActif: [
        true,
        Validators.required
      ]
    });
  }


  // ============================================================
  // CHARGEMENT
  // ============================================================

  chargerRegimesFiscaux(): void {

    this.loading = true;

    this.regimesFiscauxService
      .getRegimesFiscaux()
      .subscribe({

        next: (data: RegimeFiscalDto[]) => {

          this.regimesFiscaux =
            Array.isArray(data)
              ? [...data]
              : [];

          this.loading = false;
        },

        error: (err: HttpErrorResponse) => {

          this.loading = false;

          this.traiterErreurHttp(
            err,
            'chargement'
          );
        }
      });
  }


  // ============================================================
  // RECHERCHE ET FILTRES
  // ============================================================

  get regimesFiltres(): RegimeFiscalDto[] {

    const terme = this.recherche
      .trim()
      .toLowerCase();

    return this.regimesFiscaux.filter(regime => {

      const correspondRecherche =
        !terme ||
        regime.code?.toLowerCase().includes(terme) ||
        regime.libelle?.toLowerCase().includes(terme) ||
        regime.description?.toLowerCase().includes(terme);

      const correspondStatut =
        this.filtreActif === null ||
        regime.estActif === this.filtreActif;

      return correspondRecherche && correspondStatut;
    });
  }


  onFiltreActifChange(value: boolean | null): void {

    this.filtreActif = value;
  }


  reinitialiserFiltres(): void {

    this.recherche = '';

    this.filtreActif = null;
  }


  // ============================================================
  // AFFICHAGE DU STATUT
  // ============================================================

  getStatutLabel(estActif: boolean): string {

    return estActif ? 'Actif' : 'Inactif';
  }


  getSeverity(
    estActif: boolean
  ): 'success' | 'secondary' {

    return estActif ? 'success' : 'secondary';
  }


  // ============================================================
  // CRÉATION
  // ============================================================

  openNew(): void {

    if (!this.canManageRegimesFiscaux) {

      this.afficherAccesRefuse();

      return;
    }

    this.isEditMode = false;

    this.selectedRegimeFiscalId = undefined;

    this.regimeFiscalForm.reset({

      code: '',

      libelle: '',

      description: '',

      estActif: true
    });

    this.regimeFiscalDialog = true;
  }


  // ============================================================
  // MODIFICATION
  // ============================================================

  editRegimeFiscal(
    regime: RegimeFiscalDto
  ): void {

    if (!this.canManageRegimesFiscaux) {

      this.afficherAccesRefuse();

      return;
    }

    if (!regime.id) {

      this.messageService.add({

        severity: 'error',

        summary: 'Erreur',

        detail:
          'L’identifiant du régime fiscal est introuvable.'
      });

      return;
    }

    this.isEditMode = true;

    this.selectedRegimeFiscalId = regime.id;

    this.regimeFiscalForm.reset({

      code: regime.code ?? '',

      libelle: regime.libelle ?? '',

      description: regime.description ?? '',

      estActif: regime.estActif
    });

    this.regimeFiscalDialog = true;
  }


  // ============================================================
  // SAUVEGARDE
  // ============================================================

  saveRegimeFiscal(): void {

    if (!this.canManageRegimesFiscaux) {

      this.afficherAccesRefuse();

      return;
    }

    if (this.saving) {
      return;
    }

    if (this.regimeFiscalForm.invalid) {

      this.regimeFiscalForm.markAllAsTouched();

      return;
    }

    const formValue =
      this.regimeFiscalForm.getRawValue();

    const payload: CreerRegimeFiscalRequest = {

      code: String(
        formValue.code ?? ''
      ).trim(),

      libelle: String(
        formValue.libelle ?? ''
      ).trim(),

      description: String(
        formValue.description ?? ''
      ).trim(),

      estActif: Boolean(
        formValue.estActif
      )
    };

    if (
      !payload.code ||
      !payload.libelle
    ) {

      this.regimeFiscalForm.markAllAsTouched();

      return;
    }

    this.saving = true;

    // ----------------------------------------------------------
    // MODIFICATION
    // ----------------------------------------------------------

    if (this.isEditMode) {

      if (!this.selectedRegimeFiscalId) {

        this.saving = false;

        this.messageService.add({

          severity: 'error',

          summary: 'Erreur',

          detail:
            'L’identifiant du régime fiscal est manquant.'
        });

        return;
      }

      const request: ModifierRegimeFiscalRequest = payload;

      this.regimesFiscauxService
        .updateRegimeFiscal(
          this.selectedRegimeFiscalId,
          request
        )
        .subscribe({

          next: () => {

            this.saving = false;

            this.messageService.add({

              severity: 'success',

              summary: 'Succès',

              detail:
                'Régime fiscal modifié avec succès.'
            });

            this.fermerDialog();

            this.chargerRegimesFiscaux();
          },

          error: (err: HttpErrorResponse) => {

            this.saving = false;

            this.traiterErreurHttp(
              err,
              'modification'
            );
          }
        });

      return;
    }

    // ----------------------------------------------------------
    // CRÉATION
    // ----------------------------------------------------------

    this.regimesFiscauxService
      .createRegimeFiscal(payload)
      .subscribe({

        next: () => {

          this.saving = false;

          this.messageService.add({

            severity: 'success',

            summary: 'Succès',

            detail:
              'Régime fiscal créé avec succès.'
          });

          this.fermerDialog();

          this.chargerRegimesFiscaux();
        },

        error: (err: HttpErrorResponse) => {

          this.saving = false;

          this.traiterErreurHttp(
            err,
            'création'
          );
        }
      });
  }


  // ============================================================
  // FERMETURE DU DIALOGUE
  // ============================================================

  fermerDialog(): void {

    this.regimeFiscalDialog = false;

    this.isEditMode = false;

    this.selectedRegimeFiscalId = undefined;

    this.regimeFiscalForm.reset({

      code: '',

      libelle: '',

      description: '',

      estActif: true
    });
  }


  // ============================================================
  // SUPPRESSION LOGIQUE
  // ============================================================

  deleteRegimeFiscal(
    regime: RegimeFiscalDto
  ): void {

    if (!this.canManageRegimesFiscaux) {

      this.afficherAccesRefuse();

      return;
    }

    if (!regime.id) {
      return;
    }

    this.confirmationService.confirm({

      message:
        `Voulez-vous vraiment supprimer le régime fiscal « ${regime.libelle} » ?`,

      header:
        'Confirmation de suppression',

      icon:
        'pi pi-exclamation-triangle',

      acceptLabel:
        'Oui, supprimer',

      rejectLabel:
        'Annuler',

      acceptButtonStyleClass:
        'p-button-danger',

      accept: () => {

        this.regimesFiscauxService
          .deleteRegimeFiscal(regime.id)
          .subscribe({

            next: () => {

              this.messageService.add({

                severity: 'success',

                summary: 'Succès',

                detail:
                  'Régime fiscal supprimé avec succès.'
              });

              this.chargerRegimesFiscaux();
            },

            error: (err: HttpErrorResponse) => {

              this.traiterErreurHttp(
                err,
                'suppression'
              );
            }
          });
      }
    });
  }


  // ============================================================
  // UTILITAIRES
  // ============================================================

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
      `Erreur lors du ${action} du régime fiscal :`,
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

        detail:
          'Vous ne disposez pas des droits nécessaires.'
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
        `Une erreur est survenue lors du ${action} du régime fiscal (code ${err.status || 'inconnu'}).`,

      life: 5000
    });
  }
}