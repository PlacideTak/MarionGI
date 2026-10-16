import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { HttpErrorResponse } from '@angular/common/http';

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

  private readonly demandesService = inject(DemandesVisiteService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly authService = inject(AuthService);
  private readonly http = inject(HttpClient);

  // =========================================================
  // DONNÉES
  // =========================================================

  demandes: DemandeVisiteDto[] = [];
  biensDisponibles: BienDto[] = [];
  unitesDisponibles: UniteLocativeDto[] = [];
  agentsDisponibles: UtilisateurDto[] = [];

  // =========================================================
  // FILTRES
  // =========================================================

  recherche = '';
  filtreStatut: StatutDemandeVisite | null = null;

  // =========================================================
  // ÉTAT
  // =========================================================

  chargementEnCours = false;
  chargementUnites = false;
  displayModal = false;
  isEditMode = false;
  enregistrementEnCours = false;

  // Le champ datetime-local doit recevoir une chaîne locale,
  // et non directement un objet Date.
  dateSouhaiteeInput = '';

  nouvelleDemande: DemandeVisiteDto = this.creerNouvelleDemande();

  // =========================================================
  // UTILISATEUR CONNECTÉ
  // =========================================================

  get currentUserId(): string | null {
    return this.authService.currentUser()?.id ?? null;
  }

  get currentUserRole(): string | null {
    return this.authService.currentUser()?.role ?? null;
  }

  get agentsAffiches(): UtilisateurDto[] {
    const role = this.currentUserRole?.toLowerCase();

    if (role === ROLES.Agent.toLowerCase() && this.currentUserId) {
      return this.agentsDisponibles.filter(
        agent => agent.id === this.currentUserId
      );
    }

    return this.agentsDisponibles;
  }

  // =========================================================
  // STATUTS
  // =========================================================

  readonly statutsDisponibles = [
    {
      label: 'En attente',
      value: StatutDemandeVisite.EnAttente
    },
    {
      label: 'Confirmée',
      value: StatutDemandeVisite.Confirmee
    },
    {
      label: 'Annulée',
      value: StatutDemandeVisite.Annulee
    },
    {
      label: 'Effectuée',
      value: StatutDemandeVisite.Effectuee
    }
  ];

  // =========================================================
  // CARTES DE SYNTHÈSE
  // =========================================================

  get nombreTotalDemandes(): number {
    return this.demandes.length;
  }

  get nombreDemandesEnAttente(): number {
    return this.compterParStatut(StatutDemandeVisite.EnAttente);
  }

  get nombreDemandesConfirmees(): number {
    return this.compterParStatut(StatutDemandeVisite.Confirmee);
  }

  get nombreDemandesEffectuees(): number {
    return this.compterParStatut(StatutDemandeVisite.Effectuee);
  }

  get nombreDemandesAnnulees(): number {
    return this.compterParStatut(StatutDemandeVisite.Annulee);
  }

  private compterParStatut(statut: StatutDemandeVisite): number {
    return this.demandes.filter(
      demande => Number(demande.statut) === Number(statut)
    ).length;
  }

  // =========================================================
  // DEMANDES FILTRÉES
  // =========================================================

  get demandesFiltrees(): DemandeVisiteDto[] {
    const terme = this.recherche.trim().toLocaleLowerCase();

    return this.demandes.filter(demande => {
      const prospect = (demande.nomProspect ?? '').toLocaleLowerCase();
      const telephone = (demande.telephoneProspect ?? '').toLocaleLowerCase();

      const bien = [
        demande.bien?.nom ?? '',
        demande.bien?.reference ?? '',
        demande.uniteLocative?.bienReference ?? '',
        demande.uniteLocative?.bienAdresse ?? ''
      ].join(' ').toLocaleLowerCase();

      const unite = (
        demande.uniteLocative?.reference ?? ''
      ).toLocaleLowerCase();

      const agent = [
        demande.agent?.prenom ?? '',
        demande.agent?.nom ?? ''
      ].join(' ').toLocaleLowerCase();

      const correspondRecherche =
        !terme ||
        prospect.includes(terme) ||
        telephone.includes(terme) ||
        bien.includes(terme) ||
        unite.includes(terme) ||
        agent.includes(terme);

      const correspondStatut =
        this.filtreStatut === null ||
        Number(demande.statut) === Number(this.filtreStatut);

      return correspondRecherche && correspondStatut;
    });
  }

  reinitialiserFiltres(): void {
    this.recherche = '';
    this.filtreStatut = null;
  }

  // =========================================================
  // INITIALISATION
  // =========================================================

  ngOnInit(): void {
    this.chargerDemandes();
    this.chargerBiens();
    this.chargerAgents();
  }

  private creerNouvelleDemande(): DemandeVisiteDto {
    return {
      uniteLocativeId: '',
      bienId: undefined,
      agentId: null,
      nomProspect: '',
      telephoneProspect: '',
      dateSouhaitee: new Date(),
      observations: '',
      statut: StatutDemandeVisite.EnAttente
    };
  }

  // =========================================================
  // DATE
  // =========================================================

  get minDateString(): string {
    return this.convertirDateEnLocalInput(new Date());
  }

  private convertirDateEnLocalInput(date: Date): string {
    const decalage = date.getTimezoneOffset() * 60_000;

    return new Date(date.getTime() - decalage)
      .toISOString()
      .slice(0, 16);
  }

  private convertirDateEnInput(
    valeur: Date | string | null | undefined
  ): string {
    if (!valeur) {
      return '';
    }

    const date = valeur instanceof Date ? valeur : new Date(valeur);

    if (Number.isNaN(date.getTime())) {
      return '';
    }

    return this.convertirDateEnLocalInput(date);
  }

  get formulaireInvalide(): boolean {
    return (
      this.chargementUnites ||
      this.enregistrementEnCours ||
      !this.nouvelleDemande.uniteLocativeId ||
      !this.nouvelleDemande.nomProspect?.trim() ||
      !this.nouvelleDemande.telephoneProspect?.trim() ||
      !this.dateSouhaiteeInput
    );
  }

  // =========================================================
  // CHARGER LES DEMANDES
  // =========================================================

  chargerDemandes(): void {
    this.chargementEnCours = true;

    this.demandesService.getDemandesVisite().subscribe({
      next: data => {
        this.demandes = Array.isArray(data) ? data : [];
        this.chargementEnCours = false;
      },
      error: (err: HttpErrorResponse) => {
        console.error('Erreur chargement demandes visite :', err);
        this.chargementEnCours = false;

        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: 'Impossible de charger les demandes de visite.'
        });
      }
    });
  }

  // =========================================================
  // CHARGER LES BIENS
  // =========================================================

  chargerBiens(): void {
    this.http.get<BienDto[]>(
      `${environment.apiUrl}/BienImmobilier`
    ).subscribe({
      next: data => {
        this.biensDisponibles = Array.isArray(data)
          ? data.filter(bien => !bien.estSupprime)
          : [];
      },
      error: err => {
        console.error('Erreur chargement des biens :', err);
        this.biensDisponibles = [];

        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: 'Impossible de charger les biens immobiliers.'
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

    if (!selectedUniteId) {
      this.nouvelleDemande.uniteLocativeId = '';
    }

    this.http.get<UniteLocativeDto[]>(
      `${environment.apiUrl}/UnitesLocatives/bien/${bienId}`
    ).subscribe({
      next: data => {
        this.unitesDisponibles = Array.isArray(data)
          ? data.filter(unite => !unite.estSupprime)
          : [];

        if (selectedUniteId) {
          const uniteExiste = this.unitesDisponibles.some(
            unite => unite.id === selectedUniteId
          );

          this.nouvelleDemande.uniteLocativeId =
            uniteExiste ? selectedUniteId : '';

          if (!uniteExiste) {
            console.warn(
              'L’unité locative actuelle n’existe plus dans ce bien.',
              { selectedUniteId, bienId }
            );
          }
        }

        this.chargementUnites = false;
      },
      error: err => {
        console.error('Erreur chargement unités :', err);
        this.unitesDisponibles = [];
        this.nouvelleDemande.uniteLocativeId = '';
        this.chargementUnites = false;

        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: 'Impossible de charger les unités locatives.'
        });
      }
    });
  }

  onBienChange(bienId: string | null): void {
    this.nouvelleDemande.bienId = bienId || undefined;
    this.nouvelleDemande.uniteLocativeId = '';
    this.unitesDisponibles = [];

    if (!bienId) {
      this.chargementUnites = false;
      return;
    }

    this.chargerUnitesDuBien(bienId);
  }

  onUniteChange(uniteId: string | undefined): void {
    this.nouvelleDemande.uniteLocativeId = uniteId || '';
  }

  // =========================================================
  // CHARGER LES AGENTS
  // =========================================================

  chargerAgents(): void {
    this.http.get<UtilisateurDto[]>(
      `${environment.apiUrl}/Utilisateurs`
    ).subscribe({
      next: data => {
        this.agentsDisponibles = Array.isArray(data)
          ? data.filter(
              utilisateur =>
                utilisateur.role === RoleUtilisateur.Agent
            )
          : [];

        this.ajouterAgentConnecteSiAbsent();
      },
      error: err => {
        console.warn('Impossible de charger les agents :', err);
        this.agentsDisponibles = [];
        this.ajouterAgentConnecteSiAbsent();
      }
    });
  }

  
  private ajouterAgentConnecteSiAbsent(): void {
  const currentUser = this.authService.currentUser();

  if (
    !currentUser ||
    this.currentUserRole?.toLowerCase() !== ROLES.Agent.toLowerCase() ||
    this.agentsDisponibles.some(agent => agent.id === currentUser.id)
  ) {
    return;
  }

  const agentConnecte: UtilisateurDto = {
    id: currentUser.id,
    nom: currentUser.nom,
    prenom: currentUser.prenom,
    email: currentUser.email,
    role: RoleUtilisateur.Agent,
    telephone: '',
    statut: true,
    societeId: currentUser.societeId ?? '',

    // Propriétés obligatoires du modèle UtilisateurDto
    nomSociete: '',
    derniereConnexion: null,
    telephoneVerifie: false,
    dateDerniereModificationProfil: null
  };

  this.agentsDisponibles = [
    ...this.agentsDisponibles,
    agentConnecte
  ];
}

  // =========================================================
  // CRÉATION
  // =========================================================

  ouvrirModalCreation(): void {
    this.isEditMode = false;
    this.unitesDisponibles = [];
    this.chargementUnites = false;
    this.enregistrementEnCours = false;

    this.nouvelleDemande = this.creerNouvelleDemande();
    this.dateSouhaiteeInput = this.convertirDateEnInput(
      this.nouvelleDemande.dateSouhaitee
    );

    if (
      this.currentUserRole?.toLowerCase() === ROLES.Agent.toLowerCase() &&
      this.currentUserId
    ) {
      this.nouvelleDemande.agentId = this.currentUserId;
    }

    this.displayModal = true;
  }

  // =========================================================
  // MODIFICATION
  // =========================================================

  ouvrirModalModification(demande: DemandeVisiteDto): void {
    if (!demande) {
      return;
    }

    this.isEditMode = true;
    this.unitesDisponibles = [];
    this.chargementUnites = false;
    this.enregistrementEnCours = false;

    const bienId =
      demande.bienId ??
      demande.bien?.id ??
      demande.uniteLocative?.bienImmobilierId ??
      undefined;

    const uniteLocativeId =
      demande.uniteLocativeId ??
      demande.uniteLocative?.id ??
      '';

    this.nouvelleDemande = {
      id: demande.id,
      uniteLocativeId,
      bienId,
      agentId: demande.agentId ?? null,
      nomProspect: demande.nomProspect ?? '',
      telephoneProspect: demande.telephoneProspect ?? '',
      dateSouhaitee: demande.dateSouhaitee,
      observations: demande.observations ?? '',
      statut: demande.statut ?? StatutDemandeVisite.EnAttente
    };

    this.dateSouhaiteeInput = this.convertirDateEnInput(
      demande.dateSouhaitee
    );

    if (bienId) {
      this.chargerUnitesDuBien(bienId, uniteLocativeId);
    } else {
      console.warn(
        'Impossible de déterminer le bien associé à la demande.',
        demande
      );
      this.unitesDisponibles = [];
    }

    this.displayModal = true;
  }

  // =========================================================
  // FERMER LA MODALE
  // =========================================================

  fermerModal(): void {
    this.displayModal = false;
    this.chargementUnites = false;
    this.unitesDisponibles = [];
    this.enregistrementEnCours = false;
    this.isEditMode = false;
  }

  // =========================================================
  // ENREGISTRER
  // =========================================================

  enregistrerDemande(): void {
    if (this.enregistrementEnCours) {
      return;
    }

    if (!this.nouvelleDemande.uniteLocativeId) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Unité requise',
        detail: 'Veuillez sélectionner une unité locative.'
      });
      return;
    }

    const nomProspect = this.nouvelleDemande.nomProspect?.trim() ?? '';
    const telephoneProspect =
      this.nouvelleDemande.telephoneProspect?.trim() ?? '';

    if (!nomProspect || !telephoneProspect) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Champs requis',
        detail: 'Veuillez renseigner le nom et le téléphone du prospect.'
      });
      return;
    }

    if (!this.dateSouhaiteeInput) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Date requise',
        detail: 'Veuillez renseigner la date et l’heure souhaitées.'
      });
      return;
    }

    const dateSelectionnee = new Date(this.dateSouhaiteeInput);

    if (Number.isNaN(dateSelectionnee.getTime())) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Date invalide',
        detail: 'La date sélectionnée est invalide.'
      });
      return;
    }

    if (dateSelectionnee.getTime() < Date.now()) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Date invalide',
        detail: 'La date et l’heure ne peuvent pas être antérieures à maintenant.'
      });
      return;
    }

    const payload: DemandeVisiteDto = {
      ...(this.nouvelleDemande.id
        ? { id: this.nouvelleDemande.id }
        : {}),
      uniteLocativeId: this.nouvelleDemande.uniteLocativeId,
      agentId: this.nouvelleDemande.agentId ?? null,
      nomProspect,
      telephoneProspect,
      dateSouhaitee: dateSelectionnee,
      observations: this.nouvelleDemande.observations?.trim() || null,
      statut: this.nouvelleDemande.statut ??
        StatutDemandeVisite.EnAttente
    };

    this.enregistrementEnCours = true;

    if (this.isEditMode && this.nouvelleDemande.id) {
      this.demandesService.updateDemandeVisite(
        this.nouvelleDemande.id,
        payload
      ).subscribe({
        next: () => {
          this.fermerModal();

          this.messageService.add({
            severity: 'success',
            summary: 'Succès',
            detail: 'Demande mise à jour avec succès.'
          });

          this.chargerDemandes();
        },
        error: err => {
          this.enregistrementEnCours = false;
          console.error('Erreur mise à jour demande :', err);

          this.messageService.add({
            severity: 'error',
            summary: 'Erreur',
            detail: err?.error?.message ??
              'Impossible de modifier la demande.'
          });
        }
      });

      return;
    }

    this.demandesService.createDemandeVisite(payload).subscribe({
      next: () => {
        this.fermerModal();

        this.messageService.add({
          severity: 'success',
          summary: 'Succès',
          detail: 'Demande créée avec succès.'
        });

        this.chargerDemandes();
      },
      error: err => {
        this.enregistrementEnCours = false;
        console.error('Erreur création demande :', err);

        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: err?.error?.message ??
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

    const demandeMiseAJour: DemandeVisiteDto = {
      ...demande,
      statut: nouveauStatut
    };

    this.demandesService.updateDemandeVisite(
      demande.id,
      demandeMiseAJour
    ).subscribe({
      next: () => {
        demande.statut = nouveauStatut;

        this.messageService.add({
          severity: 'success',
          summary: 'Succès',
          detail: 'Statut mis à jour.'
        });
      },
      error: err => {
        console.error('Erreur changement statut :', err);

        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: err?.error?.message ??
            'Impossible de modifier le statut.'
        });
      }
    });
  }

  // =========================================================
  // SUPPRIMER
  // =========================================================

  supprimerDemande(id?: string): void {
    if (!id) {
      return;
    }

    this.confirmationService.confirm({
      message: 'Êtes-vous sûr de vouloir supprimer cette demande de visite ?',
      header: 'Confirmation',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Oui',
      rejectLabel: 'Non',
      acceptButtonStyleClass: 'p-button-danger',
      rejectButtonStyleClass: 'p-button-text',

      accept: () => {
        this.demandesService.deleteDemandeVisite(id).subscribe({
          next: () => {
            this.demandes = this.demandes.filter(d => d.id !== id);

            this.messageService.add({
              severity: 'success',
              summary: 'Supprimé',
              detail: 'Demande supprimée.'
            });
          },
          error: err => {
            console.error('Erreur suppression :', err);

            this.messageService.add({
              severity: 'error',
              summary: 'Erreur',
              detail: err?.error?.message ??
                'Suppression impossible.'
            });
          }
        });
      }
    });
  }

  // =========================================================
  // LIBELLÉ STATUT
  // =========================================================

  getStatutLibelle(
    statut?: StatutDemandeVisite | number
  ): string {
    switch (Number(statut)) {
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
    switch (Number(statut)) {
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