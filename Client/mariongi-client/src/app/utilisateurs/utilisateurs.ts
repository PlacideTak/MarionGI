import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormsModule,
  ReactiveFormsModule,
  FormBuilder,
  Validators
} from '@angular/forms';

import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';

import { UtilisateursService } from '../services/utilisateurs.service';
import { SocietesService } from '../services/societes.service';

import {
  UtilisateurDto,
  SocieteDto,
  RoleUtilisateur,
  CreerUtilisateurRequest,
  ModifierUtilisateurRequest
} from '../models/gestimmo.models';


@Component({
  selector: 'app-utilisateurs',
  standalone: true,

  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,

    TableModule,
    ButtonModule,
    CardModule,
    TagModule,
    DialogModule,
    InputTextModule,
    SelectModule,
    CheckboxModule
  ],

  templateUrl: './utilisateurs.html',
  styleUrls: ['./utilisateurs.scss']
})
export class Utilisateurs implements OnInit {

  // ============================================================
  // Services
  // ============================================================

  private readonly utilisateursService =
    inject(UtilisateursService);

  private readonly societesService =
    inject(SocietesService);

  private readonly fb =
    inject(FormBuilder);


  // ============================================================
  // États
  // ============================================================

  readonly utilisateurs =
    signal<UtilisateurDto[]>([]);

  readonly societes =
    signal<SocieteDto[]>([]);

  readonly isLoading =
    signal<boolean>(false);

  readonly isLoadingSocietes =
    signal<boolean>(false);

  readonly displayModal =
    signal<boolean>(false);

  readonly isEditMode =
    signal<boolean>(false);

  readonly selectedUserId =
    signal<string | null>(null);


  // ============================================================
  // Rôles
  // ============================================================

  readonly rolesList = [
    {
      label: 'Administrateur',
      value: RoleUtilisateur.Administrateur
    },
    {
      label: 'Gestionnaire',
      value: RoleUtilisateur.Gestionnaire
    },
    {
      label: 'Agent commercial',
      value: RoleUtilisateur.Agent
    },
    {
      label: 'Locataire',
      value: RoleUtilisateur.Locataire
    }
  ];


  // ============================================================
  // Matrice des permissions
  // ============================================================

  readonly matrixData = [
    {
      module: 'Gestion des sociétés',
      admin: true,
      gestionnaire: false,
      agent: false,
      locataire: false
    },
    {
      module: 'Gestion des biens immobiliers',
      admin: true,
      gestionnaire: true,
      agent: true,
      locataire: true
    },
    {
      module: 'Gestion unités locatives',
      admin: true,
      gestionnaire: true,
      agent: true,
      locataire: false
    },
    {
      module: 'Gestion des demandes de visite',
      admin: true,
      gestionnaire: true,
      agent: true,
      locataire: false
    },
    {
      module: 'Gestion des contrats',
      admin: true,
      gestionnaire: true,
      agent: false,
      locataire: true
    },

    {
      module: 'Paiements & finances',
      admin: true,
      gestionnaire: true,
      agent: false,
      locataire: true
    },
    {
      module: 'Rapports',
      admin: true,
      gestionnaire: true,
      agent: false,
      locataire: false
    },
    {
      module: 'Gestion des utilisateurs',
      admin: true,
      gestionnaire: false,
      agent: false,
      locataire: false
    }
  ];


  // ============================================================
  // Formulaire
  // ============================================================

  userForm = this.fb.group({

    nom: [
      '',
      Validators.required
    ],

    prenom: [
      '',
      Validators.required
    ],

    email: [
      '',
      [
        Validators.required,
        Validators.email
      ]
    ],

    telephone: [
      '',
      Validators.required
    ],

    role: [
      RoleUtilisateur.Gestionnaire,
      Validators.required
    ],

    societeId: [
      '',
      Validators.required
    ],

    motDePasseProvisoire: [
      ''
    ],

    statut: [
      true
    ]
  });


  // ============================================================
  // Initialisation
  // ============================================================

  ngOnInit(): void {

    this.chargerUtilisateurs();

    this.chargerSocietes();
  }


  // ============================================================
  // Charger les utilisateurs
  // ============================================================

  chargerUtilisateurs(): void {

    this.isLoading.set(true);

    this.utilisateursService
      .getUtilisateurs()
      .subscribe({

        next: (data) => {

          this.utilisateurs.set(data);

          this.isLoading.set(false);
        },

        error: (error) => {

          console.error(
            'Erreur lors du chargement des utilisateurs :',
            error
          );

          this.isLoading.set(false);
        }
      });
  }


  // ============================================================
  // Charger les sociétés
  //
  // Administrateur :
  //   toutes les sociétés
  //
  // Gestionnaire :
  //   uniquement sa société
  // ============================================================

  chargerSocietes(): void {

    this.isLoadingSocietes.set(true);

    this.societesService
      .getSocietes()
      .subscribe({

        next: (data) => {

          this.societes.set(data);

          this.isLoadingSocietes.set(false);
        },

        error: (error) => {

          console.error(
            'Erreur lors du chargement des sociétés :',
            error
          );

          this.societes.set([]);

          this.isLoadingSocietes.set(false);
        }
      });
  }


  // ============================================================
  // Ouvrir modal : ajout
  // ============================================================

  ouvrirModalAjout(): void {

    this.isEditMode.set(false);

    this.selectedUserId.set(null);

    this.userForm.reset({

      nom: '',
      prenom: '',
      email: '',
      telephone: '',

      role: RoleUtilisateur.Gestionnaire,

      societeId: '',

      motDePasseProvisoire: '',

      statut: true
    });


    // Mot de passe obligatoire à la création
    this.userForm
      .get('motDePasseProvisoire')
      ?.setValidators([
        Validators.required,
        Validators.minLength(8)
      ]);

    this.userForm
      .get('motDePasseProvisoire')
      ?.updateValueAndValidity();


    this.displayModal.set(true);
  }


  // ============================================================
  // Ouvrir modal : modification
  // ============================================================

  ouvrirModalModification(
    user: UtilisateurDto
  ): void {

    this.isEditMode.set(true);

    this.selectedUserId.set(user.id);

    this.userForm.patchValue({

      nom: user.nom,

      prenom: user.prenom,

      email: user.email,

      telephone: user.telephone,

      role: user.role,

      societeId: user.societeId,

      statut: user.statut,

      motDePasseProvisoire: ''
    });


    // Mot de passe facultatif en modification
    this.userForm
      .get('motDePasseProvisoire')
      ?.clearValidators();

    this.userForm
      .get('motDePasseProvisoire')
      ?.updateValueAndValidity();


    this.displayModal.set(true);
  }


  // ============================================================
  // Enregistrer utilisateur
  // ============================================================

  enregistrerUtilisateur(): void {

    if (this.userForm.invalid)
      return;


    const formValues =
      this.userForm.getRawValue();


    // ==========================================================
    // MODIFICATION
    // ==========================================================

    if (
      this.isEditMode() &&
      this.selectedUserId()
    ) {

      const updatePayload: ModifierUtilisateurRequest = {

        nom: formValues.nom ?? '',

        prenom: formValues.prenom ?? '',

        email: formValues.email ?? '',

        telephone: formValues.telephone ?? '',

        role: Number(
          formValues.role
        ) as RoleUtilisateur,

        statut:
          formValues.statut ?? true,

        nouveauMotDePasse:
          formValues.motDePasseProvisoire?.trim() || null,

        societeId:
          formValues.societeId || null
      };


      this.utilisateursService
        .updateUtilisateur(
          this.selectedUserId()!,
          updatePayload
        )
        .subscribe({

          next: () => {

            this.displayModal.set(false);

            this.chargerUtilisateurs();
          },

          error: (error) => {

            console.error(
              'Erreur lors de la modification de l’utilisateur :',
              error
            );
          }
        });


      return;
    }


    // ==========================================================
    // CRÉATION
    // ==========================================================

    const createPayload: CreerUtilisateurRequest = {

      nom: formValues.nom ?? '',

      prenom: formValues.prenom ?? '',

      email: formValues.email ?? '',

      telephone: formValues.telephone ?? '',

      role: Number(
        formValues.role
      ) as RoleUtilisateur,

      motDePasseInitial:
        formValues.motDePasseProvisoire ?? '',

      societeId:
        formValues.societeId || null
    };


    this.utilisateursService
      .createUtilisateur(createPayload)
      .subscribe({

        next: () => {

          this.displayModal.set(false);

          this.chargerUtilisateurs();
        },

        error: (error) => {

          console.error(
            'Erreur lors de la création de l’utilisateur :',
            error
          );
        }
      });
  }


  // ============================================================
  // Activer / désactiver
  // ============================================================

  basculerStatut(
    user: UtilisateurDto
  ): void {

    const nouveauStatut =
      !user.statut;


    this.utilisateursService
      .updateStatut(
        user.id,
        nouveauStatut
      )
      .subscribe({

        next: () => {

          this.chargerUtilisateurs();
        },

        error: (error) => {

          console.error(
            'Erreur lors de la modification du statut :',
            error
          );
        }
      });
  }


  // ============================================================
  // Libellé du rôle
  // ============================================================

  getRoleLabel(
    roleId: number
  ): string {

    const role =
      this.rolesList.find(
        r => r.value === roleId
      );

    return role
      ? role.label
      : 'Inconnu';
  }
}