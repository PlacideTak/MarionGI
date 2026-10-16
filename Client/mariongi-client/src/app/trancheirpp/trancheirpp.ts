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
import { InputNumberModule } from 'primeng/inputnumber';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TextareaModule } from 'primeng/textarea';
import { LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeFr from '@angular/common/locales/fr';

registerLocaleData(localeFr);

import {
  MessageService,
  ConfirmationService
} from 'primeng/api';

// Service
import { TranchesIRPPService } from '../services/tranches-irpp.service';

// Modèles
import type {
  TrancheIRPPDto,
  CreerTrancheIRPPRequest,
  ModifierTrancheIRPPRequest
} from '../models/gestimmo.models';

@Component({
  selector: 'app-trancheirpp',
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
    TagModule,
    ToastModule,
    ConfirmDialogModule,
    TextareaModule
  ],

  providers: [
    MessageService,
    ConfirmationService
  ],

  templateUrl: './trancheirpp.html',
  styleUrl: './trancheirpp.scss'
})
export class TrancheIRPP implements OnInit {

  // ============================================================
  // DÉPENDANCES
  // ============================================================

  private readonly fb = inject(FormBuilder);

  private readonly tranchesIRPPService =
    inject(TranchesIRPPService);

  private readonly messageService =
    inject(MessageService);

  private readonly confirmationService =
    inject(ConfirmationService);


  // ============================================================
  // DONNÉES
  // ============================================================

  tranchesIRPP: TrancheIRPPDto[] = [];

  loading = false;
  saving = false;

  recherche = '';

  filtreAnnee: number | null = null;


  // ============================================================
  // PERMISSIONS
  // ============================================================

  canAccessTranchesIRPP = false;
  canManageTranchesIRPP = false;


  // ============================================================
  // DIALOGUE ET FORMULAIRE
  // ============================================================

  trancheIRPPDialog = false;

  trancheIRPPForm!: FormGroup;

  isEditMode = false;

  selectedTrancheIRPPId?: string;


  // ============================================================
  // INITIALISATION
  // ============================================================

  ngOnInit(): void {
    this.verifierPermissions();
    this.initForm();

    if (this.canAccessTranchesIRPP) {
      this.chargerTranchesIRPP();
    }
  }


  // ============================================================
  // PERMISSIONS
  // ============================================================

  private verifierPermissions(): void {
    this.canAccessTranchesIRPP =
      this.tranchesIRPPService.canAccessTranchesIRPP();

    this.canManageTranchesIRPP =
      this.tranchesIRPPService.canManageTranchesIRPP();
  }


  // ============================================================
  // FORMULAIRE
  // ============================================================

  private initForm(): void {
    this.trancheIRPPForm = this.fb.group({
      anneeFiscale: [
        new Date().getFullYear(),
        [
          Validators.required,
          Validators.min(2000),
          Validators.max(9999)
        ]
      ],

      seuilMin: [
        0,
        [
          Validators.required,
          Validators.min(0)
        ]
      ],

      seuilMax: [
        null,
        [
          Validators.min(0)
        ]
      ],

      taux: [
        null,
        [
          Validators.required,
          Validators.min(0),
          Validators.max(1)
        ]
      ],

      ordreTranche: [
        1,
        [
          Validators.required,
          Validators.min(1),
          Validators.max(999)
        ]
      ],

      description: [
        '',
        [
          Validators.required,
          Validators.maxLength(500)
        ]
      ]
    });
  }


  // ============================================================
  // CHARGEMENT
  // ============================================================

  chargerTranchesIRPP(): void {
    if (!this.canAccessTranchesIRPP) {
      this.afficherAccesRefuse();
      return;
    }

    this.loading = true;

    this.tranchesIRPPService
      .getTranchesIRPP(this.filtreAnnee ?? undefined)
      .subscribe({
        next: (data: TrancheIRPPDto[]) => {
          console.log('Réponse API :', data);
          console.log('Tranches filtrées :', this.tranchesFiltrees);
          this.tranchesIRPP = Array.isArray(data)
            ? [...data].sort(
                (a, b) =>
                  a.anneeFiscale - b.anneeFiscale ||
                  a.ordreTranche - b.ordreTranche
              )
            : [];

          this.loading = false;
        },

        error: (err: HttpErrorResponse) => {
          this.loading = false;

          this.traiterErreurHttp(err, 'chargement');
        }
      });
  }


  // ============================================================
  // RECHERCHE ET FILTRES
  // ============================================================

  get tranchesFiltrees(): TrancheIRPPDto[] {
    const terme = this.recherche.trim().toLowerCase();

    return this.tranchesIRPP.filter(tranche => {
      return (
        !terme ||
        tranche.anneeFiscale.toString().includes(terme) ||
        tranche.ordreTranche.toString().includes(terme) ||
        tranche.seuilMin.toString().includes(terme) ||
        (tranche.seuilMax !== null &&
          tranche.seuilMax.toString().includes(terme)) ||
        tranche.taux.toString().includes(terme) ||
        (tranche.description ?? '').toLowerCase().includes(terme)
      );
    });
  }


  reinitialiserFiltres(): void {
    this.recherche = '';
    this.filtreAnnee = null;
    this.chargerTranchesIRPP();
  }


  // ============================================================
  // FORMATAGE
  // ============================================================

  /**
   * Convertit le taux décimal en pourcentage d'affichage.
   * Exemple : 0.10 devient 10 %.
   */
  formaterTaux(taux: number): string {
    return `${(taux * 100).toLocaleString('fr-FR', {
      maximumFractionDigits: 4
    })} %`;
  }


  /**
   * Affiche un seuil sans plafond lorsque SeuilMax est null.
   */
  formaterSeuilMax(seuilMax: number | null): string {
    if (seuilMax === null || seuilMax === undefined) {
      return 'Sans plafond';
    }

    return seuilMax.toLocaleString('fr-FR', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 2
    });
  }


  // ============================================================
  // CRÉATION
  // ============================================================

  openNew(): void {
    if (!this.canManageTranchesIRPP) {
      this.afficherAccesRefuse();
      return;
    }

    this.isEditMode = false;
    this.selectedTrancheIRPPId = undefined;

    this.trancheIRPPForm.reset({
      anneeFiscale:
        this.filtreAnnee ?? new Date().getFullYear(),
      seuilMin: 0,
      seuilMax: null,
      taux: null,
      ordreTranche: this.prochainOrdreTranche(),
      description: ''
    });

    this.trancheIRPPDialog = true;
  }


  private prochainOrdreTranche(): number {
    const annee = this.filtreAnnee ?? new Date().getFullYear();

    const ordres = this.tranchesIRPP
      .filter(t => t.anneeFiscale === annee)
      .map(t => t.ordreTranche);

    return ordres.length > 0
      ? Math.max(...ordres) + 1
      : 1;
  }


  // ============================================================
  // MODIFICATION
  // ============================================================

  editTrancheIRPP(tranche: TrancheIRPPDto): void {
    if (!this.canManageTranchesIRPP) {
      this.afficherAccesRefuse();
      return;
    }

    if (!tranche.id) {
      this.messageService.add({
        severity: 'error',
        summary: 'Erreur',
        detail: 'L’identifiant de la tranche IRPP est introuvable.'
      });

      return;
    }

    this.isEditMode = true;
    this.selectedTrancheIRPPId = tranche.id;

    this.trancheIRPPForm.reset({
      anneeFiscale: tranche.anneeFiscale,
      seuilMin: tranche.seuilMin,
      seuilMax: tranche.seuilMax,
      taux: tranche.taux,
      ordreTranche: tranche.ordreTranche,
      description: tranche.description ?? ''
    });

    this.trancheIRPPDialog = true;
  }


  // ============================================================
  // VALIDATION COMPLÉMENTAIRE
  // ============================================================

  private validerSeuils(): boolean {
    const valeur = this.trancheIRPPForm.getRawValue();

    const seuilMin = Number(valeur.seuilMin);
    const seuilMax =
      valeur.seuilMax === null ||
      valeur.seuilMax === '' ||
      valeur.seuilMax === undefined
        ? null
        : Number(valeur.seuilMax);

    if (
      seuilMax !== null &&
      (!Number.isFinite(seuilMax) || seuilMax <= seuilMin)
    ) {
      this.messageService.add({
        severity: 'error',
        summary: 'Seuils invalides',
        detail:
          'Le seuil maximal doit être strictement supérieur au seuil minimal.'
      });

      return false;
    }

    return true;
  }


  // ============================================================
  // SAUVEGARDE
  // ============================================================

  saveTrancheIRPP(): void {
    if (!this.canManageTranchesIRPP) {
      this.afficherAccesRefuse();
      return;
    }

    if (this.saving) {
      return;
    }

    if (this.trancheIRPPForm.invalid) {
      this.trancheIRPPForm.markAllAsTouched();

      this.messageService.add({
        severity: 'warn',
        summary: 'Formulaire incomplet',
        detail: 'Vérifie les champs obligatoires et leurs valeurs.'
      });

      return;
    }

    if (!this.validerSeuils()) {
      return;
    }

    const formValue = this.trancheIRPPForm.getRawValue();

    const payload: CreerTrancheIRPPRequest = {
      anneeFiscale: Number(formValue.anneeFiscale),
      seuilMin: Number(formValue.seuilMin),

      seuilMax:
        formValue.seuilMax === null ||
        formValue.seuilMax === '' ||
        formValue.seuilMax === undefined
          ? null
          : Number(formValue.seuilMax),

      taux: Number(formValue.taux),
      ordreTranche: Number(formValue.ordreTranche),
      description: String(formValue.description ?? '').trim()
    };

    if (!payload.description) {
      this.trancheIRPPForm
        .get('description')
        ?.markAsTouched();

      return;
    }

    this.saving = true;

    // ----------------------------------------------------------
    // MODIFICATION
    // ----------------------------------------------------------

    if (this.isEditMode) {
      if (!this.selectedTrancheIRPPId) {
        this.saving = false;

        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: 'L’identifiant de la tranche IRPP est manquant.'
        });

        return;
      }

      const request: ModifierTrancheIRPPRequest = payload;

      this.tranchesIRPPService
        .updateTrancheIRPP(
          this.selectedTrancheIRPPId,
          request
        )
        .subscribe({
          next: () => {
            this.saving = false;

            this.messageService.add({
              severity: 'success',
              summary: 'Succès',
              detail: 'Tranche IRPP modifiée avec succès.'
            });

            this.fermerDialog();
            this.chargerTranchesIRPP();
          },

          error: (err: HttpErrorResponse) => {
            this.saving = false;
            this.traiterErreurHttp(err, 'modification');
          }
        });

      return;
    }

    // ----------------------------------------------------------
    // CRÉATION
    // ----------------------------------------------------------

    this.tranchesIRPPService
      .createTrancheIRPP(payload)
      .subscribe({
        next: () => {
          this.saving = false;

          this.messageService.add({
            severity: 'success',
            summary: 'Succès',
            detail: 'Tranche IRPP créée avec succès.'
          });

          this.fermerDialog();
          this.chargerTranchesIRPP();
        },

        error: (err: HttpErrorResponse) => {
          this.saving = false;
          this.traiterErreurHttp(err, 'création');
        }
      });
  }


  // ============================================================
  // SUPPRESSION
  // ============================================================

  confirmerSuppression(tranche: TrancheIRPPDto): void {
    if (!this.canManageTranchesIRPP) {
      this.afficherAccesRefuse();
      return;
    }

    if (!tranche.id) {
      this.messageService.add({
        severity: 'error',
        summary: 'Erreur',
        detail: 'L’identifiant de la tranche IRPP est introuvable.'
      });

      return;
    }

    this.confirmationService.confirm({
      header: 'Confirmation de suppression',
      message:
        `Veux-tu supprimer la tranche ${tranche.ordreTranche} ` +
        `de l’année fiscale ${tranche.anneeFiscale} ?`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Supprimer',
      rejectLabel: 'Annuler',

      accept: () => {
        this.tranchesIRPPService
          .deleteTrancheIRPP(tranche.id!)
          .subscribe({
            next: () => {
              this.messageService.add({
                severity: 'success',
                summary: 'Succès',
                detail: 'Tranche IRPP supprimée avec succès.'
              });

              this.chargerTranchesIRPP();
            },

            error: (err: HttpErrorResponse) => {
              this.traiterErreurHttp(err, 'suppression');
            }
          });
      }
    });
  }


  // ============================================================
  // FERMETURE DU DIALOGUE
  // ============================================================

  fermerDialog(): void {
    this.trancheIRPPDialog = false;
    this.trancheIRPPForm.reset();
    this.selectedTrancheIRPPId = undefined;
    this.saving = false;
  }


  // ============================================================
  // GESTION DES ERREURS
  // ============================================================

  private traiterErreurHttp(
    err: HttpErrorResponse,
    operation: string
  ): void {
    const corps = err.error;

    let detail = '';

    if (typeof corps === 'string') {
      detail = corps;
    } else if (corps && typeof corps === 'object') {
      detail =
        corps.message ??
        corps.Message ??
        corps.title ??
        corps.detail ??
        '';
    }

    if (!detail) {
      detail = err.message || 'Une erreur inattendue est survenue.';
    }

    this.messageService.add({
      severity: 'error',
      summary: `Erreur lors du ${operation}`,
      detail
    });
  }


  // ============================================================
  // ACCÈS REFUSÉ
  // ============================================================

  private afficherAccesRefuse(): void {
    this.messageService.add({
      severity: 'warn',
      summary: 'Accès refusé',
      detail:
        'Tu ne disposes pas des permissions nécessaires pour cette opération.'
    });
  }

  get nombreAnneesFiltrees(): number {
  return new Set(
    this.tranchesFiltrees.map(t => t.anneeFiscale)
  ).size;
}

compterSansPlafond(): number {
  return this.tranchesFiltrees.filter(
    t => t.seuilMax === null || t.seuilMax === undefined
  ).length;
}

}