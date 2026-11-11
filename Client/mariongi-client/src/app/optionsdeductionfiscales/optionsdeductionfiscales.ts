import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';

import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { FormsModule } from '@angular/forms';
import {
  OptionDeductionFiscaleDto,
  CreerOptionDeductionFiscaleRequest,
  ModifierOptionDeductionFiscaleRequest,
  ModeDeductionCharges,
  MODES_DEDUCTION_OPTIONS
} from '../models/gestimmo.models';

import { OptionsDeductionFiscalesService } from '../services/options-deduction-fiscales.service';

@Component({
  selector: 'app-options-deduction-fiscales',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    ConfirmDialogModule,
    DialogModule,
    InputTextModule,
    SelectModule,
    TableModule,
    TagModule,
    TextareaModule,
    ToastModule,
    FormsModule
  ],
  templateUrl: './optionsdeductionfiscales.html',
  styleUrl: './optionsdeductionfiscales.scss'
})
export class OptionsDeductionFiscales implements OnInit {

  private readonly fb = inject(FormBuilder);
  private readonly service = inject(OptionsDeductionFiscalesService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  // ============================================================
  // Données
  // ============================================================

  options: OptionDeductionFiscaleDto[] = [];

  readonly modesDeduction = MODES_DEDUCTION_OPTIONS;

  anneeFiltre = new Date().getFullYear();

  chargement = false;
  enregistrement = false;

  // ============================================================
  // Formulaire
  // ============================================================

  formulaireVisible = false;
  modeModification = false;

  optionSelectionnee: OptionDeductionFiscaleDto | null = null;

  formulaire: FormGroup = this.fb.group({
    anneeFiscale: [
      new Date().getFullYear(),
      [
        Validators.required,
        Validators.min(2000),
        Validators.max(2100)
      ]
    ],
    modeDeduction: [
      ModeDeductionCharges.Forfait30Pourcent,
      Validators.required
    ],
    observations: ['']
  });

  // ============================================================
  // Initialisation
  // ============================================================

  ngOnInit(): void {
    this.chargerOptions();
  }

  // ============================================================
  // Chargement des options
  // ============================================================

  chargerOptions(): void {
    this.chargement = true;

    this.service.getAll(this.anneeFiltre || undefined).subscribe({
      next: (resultat) => {
        this.options = resultat ?? [];
        this.chargement = false;
      },
      error: (erreur: HttpErrorResponse) => {
        this.chargement = false;
        this.afficherErreur(erreur);
      }
    });
  }

  rechercher(): void {
    this.chargerOptions();
  }

  reinitialiserFiltre(): void {
    this.anneeFiltre = new Date().getFullYear();
    this.chargerOptions();
  }

  // ============================================================
  // Création
  // ============================================================

  ouvrirCreation(): void {
    this.modeModification = false;
    this.optionSelectionnee = null;

    this.formulaire.reset({
      anneeFiscale: this.anneeFiltre || new Date().getFullYear(),
      modeDeduction: ModeDeductionCharges.Forfait30Pourcent,
      observations: ''
    });

    this.formulaireVisible = true;
  }

  // ============================================================
  // Modification
  // ============================================================

  ouvrirModification(option: OptionDeductionFiscaleDto): void {
    if (option.estVerrouillee) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Modification impossible',
        detail: 'Cette option fiscale est verrouillée.'
      });
      return;
    }

    this.modeModification = true;
    this.optionSelectionnee = option;

    this.formulaire.patchValue({
      anneeFiscale: option.anneeFiscale,
      modeDeduction: option.modeDeduction,
      observations: option.observations ?? ''
    });

    this.formulaireVisible = true;
  }

  // ============================================================
  // Enregistrement
  // ============================================================

  enregistrer(): void {
    if (this.formulaire.invalid) {
      this.formulaire.markAllAsTouched();
      return;
    }

    const valeur = this.formulaire.getRawValue();

    const request: CreerOptionDeductionFiscaleRequest = {
      anneeFiscale: Number(valeur.anneeFiscale),
      modeDeduction: Number(valeur.modeDeduction) as ModeDeductionCharges,
      observations: valeur.observations?.trim() || null
    };

    this.enregistrement = true;

    if (this.modeModification && this.optionSelectionnee) {
      this.modifierOption(
        this.optionSelectionnee.id,
        request
      );
    } else {
      this.creerOption(request);
    }
  }

  private creerOption(
    request: CreerOptionDeductionFiscaleRequest
  ): void {
    this.service.create(request).subscribe({
      next: () => {
        this.enregistrement = false;
        this.formulaireVisible = false;

        this.messageService.add({
          severity: 'success',
          summary: 'Création réussie',
          detail: 'L’option de déduction fiscale a été créée.'
        });

        this.chargerOptions();
      },
      error: (erreur: HttpErrorResponse) => {
        this.enregistrement = false;
        this.afficherErreur(erreur);
      }
    });
  }

  private modifierOption(
    id: string,
    request: ModifierOptionDeductionFiscaleRequest
  ): void {
    this.service.update(id, request).subscribe({
      next: () => {
        this.enregistrement = false;
        this.formulaireVisible = false;

        this.messageService.add({
          severity: 'success',
          summary: 'Modification réussie',
          detail: 'L’option de déduction fiscale a été modifiée.'
        });

        this.chargerOptions();
      },
      error: (erreur: HttpErrorResponse) => {
        this.enregistrement = false;
        this.afficherErreur(erreur);
      }
    });
  }

  annulerFormulaire(): void {
    this.formulaireVisible = false;
    this.optionSelectionnee = null;
    this.formulaire.reset();
  }

  // ============================================================
  // Verrouillage
  // ============================================================

  confirmerVerrouillage(option: OptionDeductionFiscaleDto): void {
    if (option.estVerrouillee) {
      return;
    }

    this.confirmationService.confirm({
      header: 'Verrouiller l’option fiscale',
      message:
        `Voulez-vous verrouiller l'option de déduction fiscale pour ${option.anneeFiscale} ? Cette opération empêchera sa modification.`,
      icon: 'pi pi-lock',
      acceptLabel: 'Verrouiller',
      rejectLabel: 'Annuler',
      accept: () => this.verrouillerOption(option)
    });
  }

  private verrouillerOption(option: OptionDeductionFiscaleDto): void {
    this.service.verrouiller(option.id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Verrouillage réussi',
          detail: 'L’option fiscale a été verrouillée.'
        });

        this.chargerOptions();
      },
      error: (erreur: HttpErrorResponse) => {
        this.afficherErreur(erreur);
      }
    });
  }

  // ============================================================
  // Suppression
  // ============================================================

  confirmerSuppression(option: OptionDeductionFiscaleDto): void {
    this.confirmationService.confirm({
      header: 'Supprimer l’option fiscale',
      message:
        `Voulez-vous vraiment supprimer l'option de déduction fiscale de ${option.anneeFiscale} ?`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Supprimer',
      rejectLabel: 'Annuler',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.supprimerOption(option)
    });
  }

  private supprimerOption(option: OptionDeductionFiscaleDto): void {
    this.service.delete(option.id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Suppression réussie',
          detail: 'L’option fiscale a été supprimée.'
        });

        this.chargerOptions();
      },
      error: (erreur: HttpErrorResponse) => {
        this.afficherErreur(erreur);
      }
    });
  }

  // ============================================================
  // Présentation
  // ============================================================

  libelleModeDeduction(mode: ModeDeductionCharges): string {
    switch (mode) {
      case ModeDeductionCharges.Forfait30Pourcent:
        return 'Forfait de 30 %';

      case ModeDeductionCharges.FraisReels:
        return 'Frais réels';

      default:
        return 'Non défini';
    }
  }

  estVerrouillee(option: OptionDeductionFiscaleDto): boolean {
    return option.estVerrouillee;
  }

  get nombreOptions(): number {
    return this.options.length;
  }

  get nombreOptionsVerrouillees(): number {
    return this.options.filter(x => x.estVerrouillee).length;
  }

  get nombreOptionsModifiables(): number {
    return this.options.filter(x => !x.estVerrouillee).length;
  }

  // ============================================================
  // Gestion des erreurs
  // ============================================================

  private afficherErreur(erreur: HttpErrorResponse): void {
    const message =
      erreur.error?.message ||
      erreur.error?.title ||
      erreur.error?.detail ||
      erreur.message ||
      'Une erreur est survenue lors de l’opération.';

    this.messageService.add({
      severity: 'error',
      summary: 'Erreur',
      detail: message,
      life: 6000
    });
  }
}
