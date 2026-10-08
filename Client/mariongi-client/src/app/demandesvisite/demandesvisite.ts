import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

import {
  MessageService,
  ConfirmationService
} from 'primeng/api';

import { HttpClient } from '@angular/common/http';

import { DemandesVisiteService } from '../services/demandes-visite.service';
import { AuthService } from '../login/auth.service';

import {
  DemandeVisiteDto,
  BienDto,
  UniteLocativeDto,
  UtilisateurDto,
  StatutDemandeVisite,
  ROLES,
  RoleUtilisateur
} from '../models/gestimmo.models';

import { environment } from '../../environments/environment.development';


@Component({
  selector: 'app-demandes-visite',
  standalone: true,

  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    ToastModule,
    InputTextModule,
    TooltipModule,
    ConfirmDialogModule,
    DialogModule
  ],

  providers: [
    MessageService,
    ConfirmationService
  ],

  templateUrl: './demandesvisite.html',
  styleUrls: ['./demandesvisite.scss']
})
export class DemandesVisite implements OnInit {

  // =========================================================
  // SERVICES
  // =========================================================

  private readonly demandesService =
    inject(DemandesVisiteService);

  private readonly messageService =
    inject(MessageService);

  private readonly confirmationService =
    inject(ConfirmationService);

  private readonly authService =
    inject(AuthService);

  private readonly http =
    inject(HttpClient);


  // =========================================================
  // DONNÉES
  // =========================================================

  demandes: DemandeVisiteDto[] = [];

  biensDisponibles: BienDto[] = [];

  unitesDisponibles: UniteLocativeDto[] = [];

  agentsDisponibles: UtilisateurDto[] = [];


  // =========================================================
  // ÉTAT
  // =========================================================

  chargementEnCours = false;

  chargementUnites = false;

  displayModal = false;

  isEditMode = false;


  // =========================================================
  // DEMANDE EN COURS
  // =========================================================

  nouvelleDemande: DemandeVisiteDto =
    this.creerNouvelleDemande();


  // =========================================================
  // UTILISATEUR CONNECTÉ
  // =========================================================

  get currentUserId(): string | null {

    return this.authService.currentUser()?.id ?? null;

  }


  get currentUserRole(): string | null {

    return this.authService.currentUser()?.role ?? null;

  }


  // =========================================================
  // AGENTS AFFICHÉS
  // =========================================================

  get agentsAffiches(): UtilisateurDto[] {

    const role =
      this.currentUserRole?.toLowerCase();

    if (
      role === ROLES.Agent.toLowerCase() &&
      this.currentUserId
    ) {

      return this.agentsDisponibles.filter(
        agent =>
          agent.id === this.currentUserId
      );

    }

    return this.agentsDisponibles;

  }


  // =========================================================
  // INITIALISATION
  // =========================================================

  ngOnInit(): void {

    this.chargerDemandes();

    this.chargerBiens();

    this.chargerAgents();

  }


  // =========================================================
  // CRÉER UNE DEMANDE VIDE
  // =========================================================

  private creerNouvelleDemande(): DemandeVisiteDto {

    return {

      uniteLocativeId: '',

      bienId: undefined,

      agentId: null,

      nomProspect: '',

      telephoneProspect: '',

      dateSouhaitee: new Date(),

      observations: '',

      statut:
        StatutDemandeVisite.EnAttente

    };

  }


  // =========================================================
  // DATE MINIMALE
  // =========================================================

  get minDateString(): string {

    const now = new Date();

    now.setMinutes(
      now.getMinutes() -
      now.getTimezoneOffset()
    );

    return now
      .toISOString()
      .slice(0, 16);

  }


  // =========================================================
  // FORMULAIRE INVALIDE
  // =========================================================

  get formulaireInvalide(): boolean {

    const nomProspect =
      this.nouvelleDemande.nomProspect?.trim() ?? '';

    const telephoneProspect =
      this.nouvelleDemande.telephoneProspect?.trim() ?? '';

    return (
      this.chargementUnites ||
      !this.nouvelleDemande.uniteLocativeId ||
      !nomProspect ||
      !telephoneProspect ||
      !this.nouvelleDemande.dateSouhaitee
    );

  }


  // =========================================================
  // CHARGER LES DEMANDES
  // =========================================================

  chargerDemandes(): void {

    this.chargementEnCours = true;

    this.demandesService
      .getDemandesVisite()
      .subscribe({

        next: (data) => {

          this.demandes =
            Array.isArray(data)
              ? data
              : [];

          this.chargementEnCours = false;

        },

        error: (err) => {

          console.error(
            'Erreur chargement demandes visite :',
            err
          );

          this.chargementEnCours = false;

          this.messageService.add({

            severity: 'error',

            summary: 'Erreur',

            detail:
              'Impossible de charger les demandes.'

          });

        }

      });

  }


  // =========================================================
  // CHARGER LES BIENS
  // =========================================================

  chargerBiens(): void {

    this.http
      .get<BienDto[]>(
        `${environment.apiUrl}/BienImmobilier`
      )
      .subscribe({

        next: (data) => {

          this.biensDisponibles =
            Array.isArray(data)
              ? data.filter(
                  bien => !bien.estSupprime
                )
              : [];

        },

        error: (err) => {

          console.error(
            'Erreur chargement des biens :',
            err
          );

          this.biensDisponibles = [];

          this.messageService.add({

            severity: 'error',

            summary: 'Erreur',

            detail:
              'Impossible de charger les biens immobiliers.'

          });

        }

      });

  }


  // =========================================================
  // CHARGER LES UNITÉS D'UN BIEN
  // =========================================================

  chargerUnitesDuBien(
    bienId: string,
    selectedUniteId?: string
  ): void {

    if (!bienId) {

      this.unitesDisponibles = [];

      this.chargementUnites = false;

      return;

    }


    this.chargementUnites = true;

    this.unitesDisponibles = [];


    /*
     * En création :
     * aucune unité n'est présélectionnée.
     *
     * En modification :
     * l'unité actuelle est conservée pendant
     * le chargement HTTP.
     */

    if (!selectedUniteId) {

      this.nouvelleDemande.uniteLocativeId = '';

    }


    this.http
      .get<UniteLocativeDto[]>(
        `${environment.apiUrl}/UnitesLocatives/bien/${bienId}`
      )
      .subscribe({

        next: (data) => {

          this.unitesDisponibles =
            Array.isArray(data)
              ? data.filter(
                  unite => !unite.estSupprime
                )
              : [];


          // ===============================================
          // RESTAURATION UNITÉ EN MODIFICATION
          // ===============================================

          if (selectedUniteId) {

            const uniteExiste =
              this.unitesDisponibles.some(
                unite =>
                  unite.id === selectedUniteId
              );


            if (uniteExiste) {

              this.nouvelleDemande.uniteLocativeId =
                selectedUniteId;

            }
            else {

              console.warn(
                'L’unité locative actuelle n’existe plus dans ce bien.',
                {
                  selectedUniteId,
                  bienId
                }
              );

              this.nouvelleDemande.uniteLocativeId =
                '';

            }

          }


          this.chargementUnites = false;

        },

        error: (err) => {

          console.error(
            'Erreur chargement unités :',
            err
          );

          this.unitesDisponibles = [];

          this.nouvelleDemande.uniteLocativeId =
            '';

          this.chargementUnites = false;

          this.messageService.add({

            severity: 'error',

            summary: 'Erreur',

            detail:
              'Impossible de charger les unités locatives.'

          });

        }

      });

  }


  // =========================================================
  // CHANGEMENT DE BIEN
  // =========================================================

  onBienChange(
    bienId: string | null
  ): void {

    this.nouvelleDemande.bienId =
      bienId || undefined;

    this.nouvelleDemande.uniteLocativeId =
      '';

    this.unitesDisponibles = [];


    if (!bienId) {

      this.chargementUnites = false;

      return;

    }


    this.chargerUnitesDuBien(
      bienId
    );

  }


  // =========================================================
  // CHANGEMENT D'UNITÉ
  // =========================================================

  onUniteChange(
    uniteId: string | undefined
  ): void {

    this.nouvelleDemande.uniteLocativeId =
      uniteId || '';

  }


  // =========================================================
  // CHARGER LES AGENTS
  // =========================================================

  chargerAgents(): void {

    this.http
      .get<UtilisateurDto[]>(
        `${environment.apiUrl}/Utilisateurs`
      )
      .subscribe({

        next: (data) => {

          const agents =
            Array.isArray(data)
              ? data.filter(
                  utilisateur =>
                    utilisateur.role ===
                    RoleUtilisateur.Agent
                )
              : [];


          this.agentsDisponibles =
            agents;


          const currentUser =
            this.authService.currentUser();


          /*
           * Si l'utilisateur connecté est un agent
           * et que l'API ne retourne pas son compte,
           * on l'ajoute localement.
           */

          if (
            currentUser &&
            this.currentUserRole?.toLowerCase() ===
              ROLES.Agent.toLowerCase() &&
            !this.agentsDisponibles.some(
              agent =>
                agent.id === currentUser.id
            )
          ) {

            this.agentsDisponibles = [

              ...this.agentsDisponibles,

              {

                id: currentUser.id,

                nom: currentUser.nom,

                prenom: currentUser.prenom,

                email: currentUser.email,

                role:
                  RoleUtilisateur.Agent,

                telephone: '',

                statut: true,

                societeId:
                  currentUser.societeId ?? ''

              }

            ];

          }

        },

        error: (err) => {

          console.warn(
            'Impossible de charger les agents :',
            err
          );


          const currentUser =
            this.authService.currentUser();


          if (
            currentUser &&
            this.currentUserRole?.toLowerCase() ===
              ROLES.Agent.toLowerCase()
          ) {

            this.agentsDisponibles = [

              {

                id: currentUser.id,

                nom: currentUser.nom,

                prenom: currentUser.prenom,

                email: currentUser.email,

                role:
                  RoleUtilisateur.Agent,

                telephone: '',

                statut: true,

                societeId:
                  currentUser.societeId ?? ''

              }

            ];

          }
          else {

            this.agentsDisponibles = [];

          }

        }

      });

  }


  // =========================================================
  // CRÉATION
  // =========================================================

  ouvrirModalCreation(): void {

    this.isEditMode = false;

    this.unitesDisponibles = [];

    this.chargementUnites = false;


    this.nouvelleDemande =
      this.creerNouvelleDemande();


    /*
     * Si l'utilisateur connecté est un agent,
     * il est automatiquement assigné.
     */

    if (
      this.currentUserRole?.toLowerCase() ===
        ROLES.Agent.toLowerCase() &&
      this.currentUserId
    ) {

      this.nouvelleDemande.agentId =
        this.currentUserId;

    }


    this.displayModal = true;

  }


  // =========================================================
  // MODIFICATION
  // =========================================================

  ouvrirModalModification(
    demande: DemandeVisiteDto
  ): void {

    if (!demande) {

      return;

    }


    this.isEditMode = true;

    this.unitesDisponibles = [];

    this.chargementUnites = false;


    // ===============================================
    // BIEN IMMOBILIER
    // ===============================================

    const bienId =
      demande.bienId
      ??
      demande.bien?.id
      ??
      demande.uniteLocative?.bienImmobilierId
      ??
      undefined;


    // ===============================================
    // UNITÉ LOCATIVE
    // ===============================================

    const uniteLocativeId =
      demande.uniteLocativeId
      ??
      demande.uniteLocative?.id
      ??
      '';


    // ===============================================
    // FORMULAIRE
    // ===============================================

    this.nouvelleDemande = {

      id:
        demande.id,

      uniteLocativeId:
        uniteLocativeId,

      bienId:
        bienId,

      agentId:
        demande.agentId ?? null,

      nomProspect:
        demande.nomProspect ?? '',

      telephoneProspect:
        demande.telephoneProspect ?? '',

      dateSouhaitee:
        demande.dateSouhaitee,

      observations:
        demande.observations ?? '',

      statut:
        demande.statut ??
        StatutDemandeVisite.EnAttente

    };


    // ===============================================
    // CHARGEMENT DES UNITÉS
    // ===============================================

    if (bienId) {

      this.chargerUnitesDuBien(
        bienId,
        uniteLocativeId
      );

    }
    else {

      console.warn(
        'Impossible de déterminer le bien associé à la demande.',
        demande
      );

      this.unitesDisponibles = [];

    }


    // ===============================================
    // OUVERTURE
    // ===============================================

    this.displayModal = true;

  }


  // =========================================================
  // FERMER LA MODALE
  // =========================================================

  fermerModal(): void {

    this.displayModal = false;

    this.chargementUnites = false;

    this.unitesDisponibles = [];

    this.isEditMode = false;

  }


  // =========================================================
  // ENREGISTRER
  // =========================================================

  enregistrerDemande(): void {

    // ===============================================
    // VALIDATION UNITÉ
    // ===============================================

    if (
      !this.nouvelleDemande.uniteLocativeId
    ) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Unité requise',

        detail:
          'Veuillez sélectionner une unité locative.'

      });

      return;

    }


    // ===============================================
    // VALIDATION PROSPECT
    // ===============================================

    const nomProspect =
      this.nouvelleDemande.nomProspect
        ?.trim() ?? '';


    const telephoneProspect =
      this.nouvelleDemande.telephoneProspect
        ?.trim() ?? '';


    if (
      !nomProspect ||
      !telephoneProspect
    ) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Champs requis',

        detail:
          'Veuillez renseigner le nom et le téléphone du prospect.'

      });

      return;

    }


    // ===============================================
    // VALIDATION DATE
    // ===============================================

    if (
      !this.nouvelleDemande.dateSouhaitee
    ) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Date requise',

        detail:
          'Veuillez renseigner la date et l’heure souhaitées.'

      });

      return;

    }


    const dateSelectionnee =
      new Date(
        this.nouvelleDemande.dateSouhaitee
      );


    if (
      Number.isNaN(
        dateSelectionnee.getTime()
      )
    ) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Date invalide',

        detail:
          'La date sélectionnée est invalide.'

      });

      return;

    }


    if (
      dateSelectionnee.getTime() <
      Date.now()
    ) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Date invalide',

        detail:
          'La date et l’heure ne peuvent pas être antérieures à maintenant.'

      });

      return;

    }


    // ===============================================
    // PAYLOAD
    // ===============================================

    const payload: DemandeVisiteDto = {

      ...(this.nouvelleDemande.id
        ? {
            id:
              this.nouvelleDemande.id
          }
        : {}),

      uniteLocativeId:
        this.nouvelleDemande.uniteLocativeId,

      agentId:
        this.nouvelleDemande.agentId ?? null,

      nomProspect:
        nomProspect,

      telephoneProspect:
        telephoneProspect,

      dateSouhaitee:
        this.nouvelleDemande.dateSouhaitee,

      observations:
        this.nouvelleDemande.observations
          ?.trim() || null,

      statut:
        this.nouvelleDemande.statut ??
        StatutDemandeVisite.EnAttente

    };


    // ===============================================
    // MODIFICATION
    // ===============================================

    if (
      this.isEditMode &&
      this.nouvelleDemande.id
    ) {

      const id =
        this.nouvelleDemande.id;


      this.demandesService
        .updateDemandeVisite(
          id,
          payload
        )
        .subscribe({

          next: () => {

            this.fermerModal();

            this.messageService.add({

              severity: 'success',

              summary: 'Succès',

              detail:
                'Demande mise à jour avec succès.'

            });

            this.chargerDemandes();

          },

          error: (err) => {

            console.error(
              'Erreur mise à jour demande :',
              err
            );

            this.messageService.add({

              severity: 'error',

              summary: 'Erreur',

              detail:
                err?.error?.message
                ??
                'Impossible de modifier la demande.'

            });

          }

        });

      return;

    }


    // ===============================================
    // CRÉATION
    // ===============================================

    this.demandesService
      .createDemandeVisite(
        payload
      )
      .subscribe({

        next: (demandeCreee) => {

          this.fermerModal();

          if (demandeCreee) {

            this.demandes.unshift(
              demandeCreee
            );

          }

          this.messageService.add({

            severity: 'success',

            summary: 'Succès',

            detail:
              'Demande créée avec succès.'

          });

          this.chargerDemandes();

        },

        error: (err) => {

          console.error(
            'Erreur création demande :',
            err
          );

          this.messageService.add({

            severity: 'error',

            summary: 'Erreur',

            detail:
              err?.error?.message
              ??
              'Erreur lors de la création de la demande.'

          });

        }

      });

  }


  // =========================================================
  // CHANGER LE STATUT
  // =========================================================

  changerStatut(
    demande: DemandeVisiteDto,
    nouveauStatut: StatutDemandeVisite
  ): void {

    if (!demande?.id) {

      return;

    }


    const demandeMiseAJour:
      DemandeVisiteDto = {

      ...demande,

      statut:
        nouveauStatut

    };


    this.demandesService
      .updateDemandeVisite(
        demande.id,
        demandeMiseAJour
      )
      .subscribe({

        next: () => {

          demande.statut =
            nouveauStatut;

          this.messageService.add({

            severity: 'success',

            summary: 'Succès',

            detail:
              'Statut mis à jour.'

          });

        },

        error: (err) => {

          console.error(
            'Erreur changement statut :',
            err
          );

          this.messageService.add({

            severity: 'error',

            summary: 'Erreur',

            detail:
              err?.error?.message
              ??
              'Impossible de modifier le statut.'

          });

        }

      });

  }


  // =========================================================
  // SUPPRIMER
  // =========================================================

  supprimerDemande(
    id?: string
  ): void {

    if (!id) {

      return;

    }


    this.confirmationService.confirm({

      message:
        'Êtes-vous sûr de vouloir supprimer cette demande de visite ?',

      header:
        'Confirmation',

      icon:
        'pi pi-exclamation-triangle',

      acceptLabel:
        'Oui',

      rejectLabel:
        'Non',

      acceptButtonStyleClass:
        'p-button-danger',

      rejectButtonStyleClass:
        'p-button-text',

      accept: () => {

        this.demandesService
          .deleteDemandeVisite(id)
          .subscribe({

            next: () => {

              this.demandes =
                this.demandes.filter(
                  d => d.id !== id
                );

              this.messageService.add({

                severity: 'success',

                summary: 'Supprimé',

                detail:
                  'Demande supprimée.'

              });

            },

            error: (err) => {

              console.error(
                'Erreur suppression :',
                err
              );

              this.messageService.add({

                severity: 'error',

                summary: 'Erreur',

                detail:
                  err?.error?.message
                  ??
                  'Suppression impossible.'

              });

            }

          });

      }

    });

  }


  // =========================================================
  // STATUTS DISPONIBLES
  // =========================================================

  statutsDisponibles = [

    {
      label: 'En attente',
      value:
        StatutDemandeVisite.EnAttente
    },

    {
      label: 'Confirmée',
      value:
        StatutDemandeVisite.Confirmee
    },

    {
      label: 'Annulée',
      value:
        StatutDemandeVisite.Annulee
    },

    {
      label: 'Effectuée',
      value:
        StatutDemandeVisite.Effectuee
    }

  ];


  // =========================================================
  // LIBELLÉ STATUT
  // =========================================================

  getStatutLibelle(
    statut?: StatutDemandeVisite | number
  ): string {

    switch (statut) {

      case StatutDemandeVisite.EnAttente:
        return 'En attente';

      case StatutDemandeVisite.Confirmee:
        return 'Confirmée';

      case StatutDemandeVisite.Annulee:
        return 'Annulée';

      case StatutDemandeVisite.Effectuee:
        return 'Effectuée';

      default:
        return 'Inconnu';

    }

  }


  // =========================================================
  // CLASSE CSS STATUT
  // =========================================================

  getStatutBadgeClass(
    statut?: StatutDemandeVisite | number
  ): string {

    switch (statut) {

      case StatutDemandeVisite.EnAttente:
        return 'statut-en-attente';

      case StatutDemandeVisite.Confirmee:
        return 'statut-confirmee';

      case StatutDemandeVisite.Annulee:
        return 'statut-annulee';

      case StatutDemandeVisite.Effectuee:
        return 'statut-effectuee';

      default:
        return 'statut-inconnu';

    }

  }

}