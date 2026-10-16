
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
  // SERVICES
  // ============================================================

  private readonly utilisateursService =
    inject(UtilisateursService);

  private readonly societesService =
    inject(SocietesService);

  private readonly fb =
    inject(FormBuilder);

  // ============================================================
  // ÉTATS
  // ============================================================

  readonly utilisateurs = signal<UtilisateurDto[]>([]);
  readonly societes = signal<SocieteDto[]>([]);

  readonly isLoading = signal(false);
  readonly isLoadingSocietes = signal(false);
  readonly isSaving = signal(false);

  readonly displayModal = signal(false);
  readonly isEditMode = signal(false);
  readonly selectedUserId = signal<string | null>(null);

  readonly erreurChargement = signal<string | null>(null);
  readonly erreurSocietes = signal<string | null>(null);
  readonly erreurFormulaire = signal<string | null>(null);
  readonly succesMessage = signal<string | null>(null);

  // ============================================================
  // RÔLES
  // Les valeurs doivent correspondre à l'enum C#.
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
  // MATRICE DES PERMISSIONS
  // Cette matrice est informative : l'API reste l'autorité.
  // ============================================================


readonly matrixData = [
  {
    module: 'Sociétés',
    admin: true,
    gestionnaire: false,
    agent: false,
    locataire: false
  },
  {
    module: 'Biens immobiliers',
    admin: true,
    gestionnaire: true,
    agent: true,
    locataire: true
  },
  {
    module: 'Unités locatives',
    admin: true,
    gestionnaire: true,
    agent: true,
    locataire: false
  },
  {
    module: 'Demandes de visite',
    admin: true,
    gestionnaire: true,
    agent: true,
    locataire: false
  },
  {
    module: 'Contrats',
    admin: true,
    gestionnaire: true,
    agent: false,
    locataire: true
  },
  {
    module: 'Paiements et finances',
    admin: true,
    gestionnaire: true,
    agent: false,
    locataire: true
  },
  {
    module: 'Prestataires',
    admin: true,
    gestionnaire: true,
    agent: false,
    locataire: false
  },
  {
    module: 'Charges immobilières',
    admin: true,
    gestionnaire: true,
    agent: false,
    locataire: false
  },
  {
    module: 'Tranches IRPP',
    admin: true,
    gestionnaire: true,
    agent: false,
    locataire: false
  },
  {
    module: 'Paramètres fiscaux',
    admin: true,
    gestionnaire: true,
    agent: false,
    locataire: false
  },
  {
    module: 'Régimes fiscaux',
    admin: true,
    gestionnaire: true,
    agent: false,
    locataire: false
  },
  {
    module: 'Valeurs fiscales',
    admin: true,
    gestionnaire: true,
    agent: false,
    locataire: false
  },
  {
    module: 'Déclarations fiscales',
    admin: true,
    gestionnaire: true,
    agent: false,
    locataire: false
  },
  {
    module: 'Rapports',
    admin: true,
    gestionnaire: true,
    agent: false,
    locataire: false
  },
  {
    module: 'Utilisateurs',
    admin: true,
    gestionnaire: true,
    agent: false,
    locataire: false
  }
];

  // ============================================================
  // FORMULAIRE
  // ============================================================

  readonly userForm = this.fb.group({
    nom: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.maxLength(100)
    ]),

    prenom: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.maxLength(100)
    ]),

    email: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.email,
      Validators.maxLength(254)
    ]),

    telephone: this.fb.nonNullable.control('', [
      Validators.maxLength(30)
    ]),

    role: this.fb.nonNullable.control(
      RoleUtilisateur.Gestionnaire,
      Validators.required
    ),

    societeId: this.fb.nonNullable.control('', [
      Validators.required
    ]),

    motDePasseProvisoire: this.fb.nonNullable.control(''),

    statut: this.fb.nonNullable.control(true)
  });

  // ============================================================
  // INITIALISATION
  // ============================================================

  ngOnInit(): void {
    this.chargerUtilisateurs();
    this.chargerSocietes();
  }

  // ============================================================
  // CHARGEMENT DES UTILISATEURS
  // ============================================================

  chargerUtilisateurs(): void {
    this.isLoading.set(true);
    this.erreurChargement.set(null);

    this.utilisateursService.getUtilisateurs().subscribe({
      next: (data) => {
        this.utilisateurs.set(data);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error(
          'Erreur lors du chargement des utilisateurs :',
          error
        );

        this.erreurChargement.set(
          this.extraireMessageErreur(
            error,
            'Impossible de charger les utilisateurs.'
          )
        );

        this.isLoading.set(false);
      }
    });
  }

  // ============================================================
  // CHARGEMENT DES SOCIÉTÉS
  // ============================================================

  chargerSocietes(): void {
    this.isLoadingSocietes.set(true);
    this.erreurSocietes.set(null);

    this.societesService.getSocietes().subscribe({
      next: (data) => {
        // Ne proposer que les sociétés actives.
        this.societes.set(
          data.filter(societe => societe.actif)
        );

        this.isLoadingSocietes.set(false);
      },
      error: (error) => {
        console.error(
          'Erreur lors du chargement des sociétés :',
          error
        );

        this.societes.set([]);

        this.erreurSocietes.set(
          this.extraireMessageErreur(
            error,
            'Impossible de charger les sociétés.'
          )
        );

        this.isLoadingSocietes.set(false);
      }
    });
  }

  // ============================================================
  // OUVRIR LA MODALE : AJOUT
  // ============================================================

  ouvrirModalAjout(): void {
    this.isEditMode.set(false);
    this.selectedUserId.set(null);

    this.erreurFormulaire.set(null);
    this.succesMessage.set(null);

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

    this.userForm.controls.motDePasseProvisoire.setValidators([
      Validators.required,
      Validators.minLength(8)
    ]);

    this.userForm.controls.motDePasseProvisoire
      .updateValueAndValidity();

    this.displayModal.set(true);
  }

  // ============================================================
  // OUVRIR LA MODALE : MODIFICATION
  // ============================================================

  ouvrirModalModification(user: UtilisateurDto): void {
    this.isEditMode.set(true);
    this.selectedUserId.set(user.id);

    this.erreurFormulaire.set(null);
    this.succesMessage.set(null);

    this.userForm.reset({
      nom: user.nom,
      prenom: user.prenom,
      email: user.email,
      telephone: user.telephone ?? '',
      role: user.role,
      societeId: user.societeId,
      motDePasseProvisoire: '',
      statut: user.statut
    });

    // L'API actuelle ne permet pas de modifier le mot de passe
    // via le PUT : ce champ est donc désactivé fonctionnellement.
    this.userForm.controls.motDePasseProvisoire.clearValidators();
    this.userForm.controls.motDePasseProvisoire
      .updateValueAndValidity();

    this.displayModal.set(true);
  }

  // ============================================================
  // ENREGISTRER
  // ============================================================

  enregistrerUtilisateur(): void {
    this.erreurFormulaire.set(null);
    this.succesMessage.set(null);

    if (this.isSaving()) {
      return;
    }

    if (this.userForm.invalid) {
      this.userForm.markAllAsTouched();

      this.erreurFormulaire.set(
        'Veuillez corriger les champs obligatoires avant de continuer.'
      );

      return;
    }

    const formValues = this.userForm.getRawValue();

    const nom = formValues.nom.trim();
    const prenom = formValues.prenom.trim();
    const email = formValues.email.trim();
    const telephone = formValues.telephone.trim();
    const societeId = formValues.societeId;

    if (!societeId) {
      this.erreurFormulaire.set(
        'Veuillez sélectionner une société.'
      );
      return;
    }

    if (
      !this.societes().some(
        societe => societe.id === societeId && societe.actif
      )
    ) {
      this.erreurFormulaire.set(
        'La société sélectionnée est introuvable ou inactive. Actualisez la liste des sociétés.'
      );
      return;
    }

    const role = Number(formValues.role) as RoleUtilisateur;

    // ==========================================================
    // MODIFICATION
    // ==========================================================

    if (this.isEditMode()) {
      const userId = this.selectedUserId();

      if (!userId) {
        this.erreurFormulaire.set(
          'Impossible de déterminer l’utilisateur à modifier.'
        );
        return;
      }

      const updatePayload: ModifierUtilisateurRequest = {
        nom,
        prenom,
        email,
        telephone,
        role,
        societeId
      };

      this.isSaving.set(true);

      this.utilisateursService
        .updateUtilisateur(userId, updatePayload)
        .subscribe({
          next: () => {
            this.isSaving.set(false);
            this.displayModal.set(false);
            this.succesMessage.set(
              'Utilisateur modifié avec succès.'
            );
            this.chargerUtilisateurs();
          },
          error: (error) => {
            console.error(
              'Erreur lors de la modification de l’utilisateur :',
              error
            );

            this.erreurFormulaire.set(
              this.extraireMessageErreur(
                error,
                'Une erreur est survenue lors de la modification.'
              )
            );

            this.isSaving.set(false);
          }
        });

      return;
    }

    // ==========================================================
    // CRÉATION
    // ==========================================================

    const motDePasse = formValues.motDePasseProvisoire;

    if (motDePasse.length < 8) {
      this.userForm.controls.motDePasseProvisoire
        .markAsTouched();

      this.erreurFormulaire.set(
        'Le mot de passe doit contenir au moins 8 caractères.'
      );

      return;
    }

    const createPayload: CreerUtilisateurRequest = {
      nom,
      prenom,
      email,
      telephone,
      motDePasse,
      role,
      societeId
    };

    this.isSaving.set(true);

    this.utilisateursService
      .createUtilisateur(createPayload)
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.displayModal.set(false);
          this.succesMessage.set(
            'Utilisateur créé avec succès.'
          );
          this.chargerUtilisateurs();
        },
        error: (error) => {
          console.error(
            'Erreur lors de la création de l’utilisateur :',
            error
          );

          this.erreurFormulaire.set(
            this.extraireMessageErreur(
              error,
              'Une erreur est survenue lors de la création.'
            )
          );

          this.isSaving.set(false);
        }
      });
  }

  // ============================================================
  // ACTIVER / DÉSACTIVER
  // ============================================================

  basculerStatut(user: UtilisateurDto): void {
    // L'API refuse de modifier le statut de son propre compte.
    // Le serveur reste l'autorité pour cette règle.
    if (user.statut === undefined || !user.id) {
      return;
    }

    const nouveauStatut = !user.statut;

    this.utilisateursService
      .updateStatut(user.id, nouveauStatut)
      .subscribe({
        next: () => {
          this.succesMessage.set(
            nouveauStatut
              ? 'Utilisateur activé avec succès.'
              : 'Utilisateur désactivé avec succès.'
          );

          this.chargerUtilisateurs();
        },
        error: (error) => {
          console.error(
            'Erreur lors de la modification du statut :',
            error
          );

          this.erreurChargement.set(
            this.extraireMessageErreur(
              error,
              'Impossible de modifier le statut de cet utilisateur.'
            )
          );
        }
      });
  }

  // ============================================================
  // STATISTIQUES
  // ============================================================

  get totalUtilisateurs(): number {
    return this.utilisateurs().length;
  }

  get nombreAdministrateurs(): number {
    return this.compterParRole(
      RoleUtilisateur.Administrateur
    );
  }

  get nombreGestionnaires(): number {
    return this.compterParRole(
      RoleUtilisateur.Gestionnaire
    );
  }

  get nombreAgents(): number {
    return this.compterParRole(
      RoleUtilisateur.Agent
    );
  }

  get nombreLocataires(): number {
    return this.compterParRole(
      RoleUtilisateur.Locataire
    );
  }

  private compterParRole(role: RoleUtilisateur): number {
    return this.utilisateurs().filter(
      user => Number(user.role) === role
    ).length;
  }

  // ============================================================
  // LIBELLÉ DU RÔLE
  // ============================================================

  getRoleLabel(roleId: number): string {
    const role = this.rolesList.find(
      item => item.value === Number(roleId)
    );

    return role?.label ?? 'Inconnu';
  }

  // ============================================================
  // UTILITAIRES
  // ============================================================

  private extraireMessageErreur(
    error: any,
    messageParDefaut: string
  ): string {
    const message = error?.error;

    if (typeof message === 'string' && message.trim()) {
      return message;
    }

    if (
      message &&
      typeof message === 'object' &&
      typeof message.detail === 'string'
    ) {
      return message.detail;
    }

    if (
      message &&
      typeof message === 'object' &&
      typeof message.title === 'string'
    ) {
      return message.title;
    }

    if (
      message &&
      typeof message === 'object' &&
      typeof message.message === 'string'
    ) {
      return message.message;
    }

    return messageParDefaut;
  }
}
