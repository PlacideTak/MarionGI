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

  nouvelleDemande: DemandeVisiteDto = {

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


  // =========================================================
  // UTILISATEUR CONNECTÉ
  // =========================================================

  get currentUserId(): string | null {

    return this.authService.currentUser()?.id || null;

  }


  get currentUserRole(): string | null {

    return this.authService.currentUser()?.role || null;

  }


  // =========================================================
  // AGENTS AFFICHÉS
  // =========================================================

  get agentsAffiches(): UtilisateurDto[] {

    if (
      this.currentUserRole?.toLowerCase() ===
        ROLES.Agent.toLowerCase() &&
      this.currentUserId
    ) {

      return this.agentsDisponibles.filter(
        agent => agent.id === this.currentUserId
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
  // CHARGER LES DEMANDES
  // =========================================================

  chargerDemandes(): void {

    this.chargementEnCours = true;

    this.demandesService
      .getDemandesVisite()
      .subscribe({

        next: (data) => {

          this.demandes =
            data || [];

          this.chargementEnCours =
            false;

        },

        error: (err) => {

          console.error(
            'Erreur chargement demandes visite :',
            err
          );

          this.chargementEnCours =
            false;

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
            data || [];

        },

        error: (err) => {

          console.error(
            'Erreur chargement des biens :',
            err
          );

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
  //
  // selectedUniteId est facultatif.
  //
  // - Création :
  //     aucun selectedUniteId
  //     → aucune unité sélectionnée
  //
  // - Modification :
  //     selectedUniteId = unité actuelle
  //     → l'unité est restaurée après le chargement
  //
  // =========================================================

  chargerUnitesDuBien(
    bienId: string,
    selectedUniteId?: string
  ): void {

    this.unitesDisponibles = [];

    this.chargementUnites = true;

    // -------------------------------------------------------
    // En création, on réinitialise l'unité.
    //
    // En modification, on conserve temporairement
    // l'identifiant afin de le restaurer après le chargement.
    // -------------------------------------------------------

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
            (data || []).filter(
              unite =>
                !unite.estSupprime
            );


          // ---------------------------------------------------
          // RESTAURATION DE L'UNITÉ EN MODE MODIFICATION
          // ---------------------------------------------------

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
                'L’unité locative associée à la demande n’a pas été trouvée parmi les unités du bien.',
                {
                  uniteLocativeId: selectedUniteId,
                  bienId
                }
              );

              this.nouvelleDemande.uniteLocativeId =
                '';

            }

          }


          this.chargementUnites =
            false;

        },

        error: (err) => {

          console.error(
            'Erreur chargement unités :',
            err
          );

          this.unitesDisponibles =
            [];

          this.nouvelleDemande.uniteLocativeId =
            '';

          this.chargementUnites =
            false;

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

    this.unitesDisponibles = [];

    this.nouvelleDemande.uniteLocativeId =
      '';

    this.nouvelleDemande.bienId =
      bienId || undefined;


    if (!bienId) {

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

          this.agentsDisponibles =
            (data || []).filter(
              utilisateur =>
                utilisateur.role ===
                RoleUtilisateur.Agent
            );

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

                role: RoleUtilisateur.Agent,

                telephone: '',

                statut: true,

                societeId:
                  currentUser.societeId ?? ''

              }

            ];

          }
          else {

            this.agentsDisponibles =
              [];

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

    this.nouvelleDemande = {

      uniteLocativeId: '',

      bienId: undefined,

      agentId: null,

      nomProspect: '',

      telephoneProspect: '',

      dateSouhaitee:
        new Date(),

      observations: '',

      statut:
        StatutDemandeVisite.EnAttente

    };


    // -------------------------------------------------------
    // Agent connecté
    // -------------------------------------------------------

    if (
      this.currentUserRole?.toLowerCase() ===
        ROLES.Agent.toLowerCase() &&
      this.currentUserId
    ) {

      this.nouvelleDemande.agentId =
        this.currentUserId;

    }


    this.displayModal =
      true;

  }


  // =========================================================
  // MODIFICATION
  // =========================================================

  ouvrirModalModification(
    demande: DemandeVisiteDto
  ): void {

    this.isEditMode = true;


    // -------------------------------------------------------
    // Récupération du bien parent
    // -------------------------------------------------------

    const bienId =
      demande.uniteLocative?.bienImmobilierId ??
      demande.bienId;


    // -------------------------------------------------------
    // Récupération de l'unité actuelle
    // -------------------------------------------------------

    const uniteLocativeId =
      demande.uniteLocativeId;


    // -------------------------------------------------------
    // Préparation du formulaire
    // -------------------------------------------------------

    this.nouvelleDemande = {

      id: demande.id,

      uniteLocativeId:
        uniteLocativeId,

      bienId:
        bienId,

      agentId:
        demande.agentId || null,

      nomProspect:
        demande.nomProspect,

      telephoneProspect:
        demande.telephoneProspect,

      dateSouhaitee:
        demande.dateSouhaitee,

      observations:
        demande.observations ?? '',

      statut:
        demande.statut ??
        StatutDemandeVisite.EnAttente

    };


    // -------------------------------------------------------
    // Charger les unités du bien
    //
    // IMPORTANT :
    // On passe l'identifiant de l'unité actuelle.
    // Il sera restauré APRÈS le chargement HTTP.
    // -------------------------------------------------------

    if (bienId) {

      this.chargerUnitesDuBien(
        bienId,
        uniteLocativeId
      );

    }
    else {

      console.warn(
        'Impossible de déterminer le bien associé à la demande de visite.',
        demande
      );

      this.unitesDisponibles = [];

    }


    this.displayModal =
      true;

  }


  // =========================================================
  // ENREGISTRER
  // =========================================================

  enregistrerDemande(): void {

    // -------------------------------------------------------
    // Validation unité
    // -------------------------------------------------------

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


    // -------------------------------------------------------
    // Validation prospect
    // -------------------------------------------------------

    if (
      !this.nouvelleDemande.nomProspect?.trim() ||
      !this.nouvelleDemande.telephoneProspect?.trim()
    ) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Champs requis',

        detail:
          'Veuillez renseigner le nom et le téléphone du prospect.'

      });

      return;

    }


    // -------------------------------------------------------
    // Validation date
    // -------------------------------------------------------

    if (
      !this.nouvelleDemande.dateSouhaitee
    ) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Date requise',

        detail:
          'Veuillez renseigner la date et l\'heure souhaitées.'

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
      dateSelectionnee <
      new Date()
    ) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Date invalide',

        detail:
          'La date et l\'heure ne peuvent pas être antérieures à maintenant.'

      });

      return;

    }


    // =======================================================
    // PAYLOAD
    // =======================================================

    const payload: DemandeVisiteDto = {

      uniteLocativeId:
        this.nouvelleDemande.uniteLocativeId,

      agentId:
        this.nouvelleDemande.agentId ||
        null,

      nomProspect:
        this.nouvelleDemande.nomProspect.trim(),

      telephoneProspect:
        this.nouvelleDemande.telephoneProspect.trim(),

      dateSouhaitee:
        this.nouvelleDemande.dateSouhaitee,

      observations:
        this.nouvelleDemande.observations?.trim() ||
        null,

      statut:
        this.nouvelleDemande.statut ??
        StatutDemandeVisite.EnAttente

    };


    // =======================================================
    // MODIFICATION
    // =======================================================

    if (
      this.isEditMode &&
      this.nouvelleDemande.id
    ) {

      this.demandesService
        .updateDemandeVisite(

          this.nouvelleDemande.id,

          {
            ...payload,

            id:
              this.nouvelleDemande.id

          }

        )
        .subscribe({

          next: () => {

            this.displayModal =
              false;

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
              'Erreur mise à jour :',
              err
            );

            this.messageService.add({

              severity: 'error',

              summary: 'Erreur',

              detail:
                'Impossible de modifier la demande.'

            });

          }

        });

      return;

    }


    // =======================================================
    // CRÉATION
    // =======================================================

    this.demandesService
      .createDemandeVisite(
        payload
      )
      .subscribe({

        next: (demandeCreee) => {

          this.demandes.unshift(
            demandeCreee
          );

          this.displayModal =
            false;

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
            'Erreur création :',
            err
          );

          this.messageService.add({

            severity: 'error',

            summary: 'Erreur',

            detail:
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

    if (!demande.id) {

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
                  'Suppression impossible.'

              });

            }

          });

      }

    });

  }


  // =========================================================
  // STATUTS
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