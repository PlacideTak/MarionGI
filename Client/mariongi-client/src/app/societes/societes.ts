import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  Validators
} from '@angular/forms';

import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';

import { SocietesService } from '../services/societes.service';

import {
  SocieteDto,
  ModifierSocieteRequest
} from '../models/gestimmo.models';

@Component({
  selector: 'app-societes',
  standalone: true,

  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    CardModule,
    TagModule,
    DialogModule,
    InputTextModule,
    TableModule,
    ConfirmDialogModule
  ],

  providers: [
    ConfirmationService
  ],

  templateUrl: './societes.html',
  styleUrls: ['./societes.scss']
})
export class Societes implements OnInit {

  private readonly societesService = inject(SocietesService);
  private readonly fb = inject(FormBuilder);
  private readonly confirmationService =
    inject(ConfirmationService);

  // ============================================================
  // DONNÉES
  // ============================================================

  readonly societes = signal<SocieteDto[]>([]);

  readonly isLoading = signal<boolean>(false);

  readonly displayModal = signal<boolean>(false);

  readonly isEditMode = signal<boolean>(false);

  readonly selectedSociete =
    signal<SocieteDto | null>(null);

  readonly errorMessage = signal<string>('');

  readonly successMessage = signal<string>('');


  // ============================================================
  // FORMULAIRE
  // ============================================================

  readonly societeForm = this.fb.group({
    nom: ['', Validators.required],
    numeroEntreprise: [''],
    adresse: [''],
    ville: [''],
    codePostal: [''],
    telephone: [''],
    email: ['', Validators.email]
  });


  // ============================================================
  // INITIALISATION
  // ============================================================

  ngOnInit(): void {
    this.chargerSocietes();
  }


  // ============================================================
  // CHARGEMENT
  // ============================================================

  chargerSocietes(): void {

    this.isLoading.set(true);
    this.errorMessage.set('');

    this.societesService
      .getSocietes()
      .subscribe({

        next: (data) => {

          this.societes.set(
            Array.isArray(data) ? data : []
          );

          this.isLoading.set(false);
        },

        error: (err) => {

          console.error(
            'Erreur lors du chargement des sociétés :',
            err
          );

          this.errorMessage.set(
            err?.error ||
            'Impossible de charger les sociétés.'
          );

          this.isLoading.set(false);
        }

      });
  }


  // ============================================================
  // NOUVELLE SOCIÉTÉ
  // ============================================================

  ouvrirModalAjout(): void {

    this.isEditMode.set(false);
    this.selectedSociete.set(null);

    this.societeForm.reset({
      nom: '',
      numeroEntreprise: '',
      adresse: '',
      ville: '',
      codePostal: '',
      telephone: '',
      email: ''
    });

    this.displayModal.set(true);
  }


  // ============================================================
  // MODIFICATION
  // ============================================================

  ouvrirModalModification(
    societe: SocieteDto
  ): void {

    this.isEditMode.set(true);

    this.selectedSociete.set(societe);

    this.societeForm.patchValue({
      nom: societe.nom,
      numeroEntreprise:
        societe.numeroEntreprise ?? '',
      adresse:
        societe.adresse ?? '',
      ville:
        societe.ville ?? '',
      codePostal:
        societe.codePostal ?? '',
      telephone:
        societe.telephone ?? '',
      email:
        societe.email ?? ''
    });

    this.displayModal.set(true);
  }


  // ============================================================
  // ENREGISTREMENT
  // ============================================================

  enregistrerSociete(): void {

    if (this.societeForm.invalid) {

      this.societeForm.markAllAsTouched();

      return;
    }

    const values =
      this.societeForm.getRawValue();

    const request: ModifierSocieteRequest = {

      nom:
        values.nom?.trim() ?? '',

      numeroEntreprise:
        values.numeroEntreprise?.trim() || null,

      adresse:
        values.adresse?.trim() || null,

      ville:
        values.ville?.trim() || null,

      codePostal:
        values.codePostal?.trim() || null,

      telephone:
        values.telephone?.trim() || null,

      email:
        values.email?.trim() || null
    };


    // ----------------------------------------------------------
    // MODIFICATION
    // ----------------------------------------------------------

    if (this.isEditMode()) {

      const societe =
        this.selectedSociete();

      if (!societe) {
        return;
      }

      this.isLoading.set(true);

      this.societesService
        .updateSociete(
          societe.id,
          request
        )
        .subscribe({

          next: () => {

            this.displayModal.set(false);

            this.successMessage.set(
              'Société modifiée avec succès.'
            );

            this.chargerSocietes();
          },

          error: (err) => {

            console.error(
              'Erreur lors de la modification :',
              err
            );

            this.errorMessage.set(
              err?.error ||
              'Impossible de modifier la société.'
            );

            this.isLoading.set(false);
          }

        });

      return;
    }


    // ----------------------------------------------------------
    // CRÉATION
    // ----------------------------------------------------------

    this.isLoading.set(true);

    this.societesService
      .createSociete(request)
      .subscribe({

        next: () => {

          this.displayModal.set(false);

          this.successMessage.set(
            'Société créée avec succès.'
          );

          this.chargerSocietes();
        },

        error: (err) => {

          console.error(
            'Erreur lors de la création :',
            err
          );

          this.errorMessage.set(
            err?.error ||
            'Impossible de créer la société.'
          );

          this.isLoading.set(false);
        }

      });
  }


  // ============================================================
  // ACTIVATION / DÉSACTIVATION
  // ============================================================

  basculerStatut(
    societe: SocieteDto
  ): void {

    const nouveauStatut =
      !societe.actif;

    this.confirmationService.confirm({

      message: nouveauStatut
        ? `Voulez-vous activer la société « ${societe.nom} » ?`
        : `Voulez-vous désactiver la société « ${societe.nom} » ?`,

      header: nouveauStatut
        ? 'Activer la société'
        : 'Désactiver la société',

      icon: nouveauStatut
        ? 'pi pi-check-circle'
        : 'pi pi-exclamation-triangle',

      acceptLabel: 'Oui',
      rejectLabel: 'Annuler',

      accept: () => {

        this.isLoading.set(true);

        this.societesService
          .updateStatut(
            societe.id,
            {
              actif: nouveauStatut
            }
          )
          .subscribe({

            next: () => {

              this.successMessage.set(
                nouveauStatut
                  ? 'Société activée avec succès.'
                  : 'Société désactivée avec succès.'
              );

              this.chargerSocietes();
            },

            error: (err) => {

              console.error(
                'Erreur lors de la modification du statut :',
                err
              );

              this.errorMessage.set(
                err?.error ||
                'Impossible de modifier le statut de la société.'
              );

              this.isLoading.set(false);
            }

          });
      }

    });
  }


  // ============================================================
  // SUPPRESSION
  // ============================================================

  supprimerSociete(
    societe: SocieteDto
  ): void {

    this.confirmationService.confirm({

      message:
        `Voulez-vous vraiment supprimer la société « ${societe.nom} » ?`,

      header:
        'Confirmation de suppression',

      icon:
        'pi pi-exclamation-triangle',

      acceptLabel:
        'Supprimer',

      rejectLabel:
        'Annuler',

      accept: () => {

        this.isLoading.set(true);

        this.societesService
          .deleteSociete(societe.id)
          .subscribe({

            next: () => {

              this.successMessage.set(
                'Société supprimée avec succès.'
              );

              this.chargerSocietes();
            },

            error: (err) => {

              console.error(
                'Erreur lors de la suppression :',
                err
              );

              this.errorMessage.set(
                err?.error ||
                'Impossible de supprimer la société.'
              );

              this.isLoading.set(false);
            }

          });
      }

    });
  }


  // ============================================================
  // FERMETURE MODALE
  // ============================================================

  fermerModal(): void {

    this.displayModal.set(false);

    this.societeForm.reset();
  }
}