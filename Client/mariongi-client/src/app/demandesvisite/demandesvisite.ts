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
import { MessageService, ConfirmationService } from 'primeng/api';
import { HttpClient } from '@angular/common/http';

import { DemandesVisiteService } from '../services/demandes-visite.service';
import { DemandeVisiteDto, BienDto, UtilisateurDto, StatutDemandeVisite } from '../models/gestimmo.models';
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
  providers: [MessageService, ConfirmationService],
  templateUrl: './demandesvisite.html',
  styleUrls: ['./demandesvisite.scss']
})
export class DemandesVisite implements OnInit {
  private readonly demandesService = inject(DemandesVisiteService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly http = inject(HttpClient);

  demandes: DemandeVisiteDto[] = [];
  biensDisponibles: BienDto[] = [];
  agentsDisponibles: UtilisateurDto[] = [];
  chargementEnCours: boolean = false;

  displayModal: boolean = false;
  isEditMode: boolean = false;

  nouvelleDemande: DemandeVisiteDto = {
    bienId: '',
    agentId: null,
    nomProspect: '',
    telephoneProspect: '',
    dateSouhaitee: new Date()
  };

  ngOnInit(): void {
    this.chargerDemandes();
    this.chargerBiens();
    this.chargerAgents();
  }

  get minDateString(): string {
    const now = new Date();
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
    return now.toISOString().slice(0, 16);
  }

  chargerDemandes(): void {
    this.chargementEnCours = true;
    this.demandesService.getDemandesVisite().subscribe({
      next: (data) => {
        this.demandes = data;
        this.chargementEnCours = false;
      },
      error: (err) => {
        console.error('Erreur chargement demandes visite :', err);
        this.chargementEnCours = false;
        this.messageService.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de charger les demandes.' });
      }
    });
  }

  chargerBiens(): void {
    this.http.get<BienDto[]>(`${environment.apiUrl}/Biens`).subscribe({
      next: (data) => {
        this.biensDisponibles = data;
      },
      error: (err) => {
        console.error('Erreur chargement des biens :', err);
      }
    });
  }

  chargerAgents(): void {
    this.http.get<UtilisateurDto[]>(`${environment.apiUrl}/Utilisateurs`).subscribe({
      next: (data) => {
        this.agentsDisponibles = data;
      },
      error: (err) => {
        console.error('Erreur chargement des agents :', err);
      }
    });
  }

  ouvrirModalCreation(): void {
    this.isEditMode = false;
    this.nouvelleDemande = {
      bienId: '',
      agentId: null,
      nomProspect: '',
      telephoneProspect: '',
      dateSouhaitee: new Date(),
      statut: StatutDemandeVisite.EnAttente
    };
    this.displayModal = true;
  }

  ouvrirModalModification(demande: DemandeVisiteDto): void {
    this.isEditMode = true;
    this.nouvelleDemande = {
      id: demande.id,
      bienId: demande.bienId,
      agentId: demande.agentId || null,
      nomProspect: demande.nomProspect,
      telephoneProspect: demande.telephoneProspect,
      dateSouhaitee: demande.dateSouhaitee,
      statut: demande.statut
    };
    this.displayModal = true;
  }

  enregistrerDemande(): void {
    if (!this.nouvelleDemande.bienId || !this.nouvelleDemande.nomProspect || !this.nouvelleDemande.telephoneProspect) {
      this.messageService.add({ severity: 'warn', summary: 'Champs requis', detail: 'Veuillez remplir tous les champs obligatoires.' });
      return;
    }

    if (!this.nouvelleDemande.dateSouhaitee) {
      this.messageService.add({ severity: 'warn', summary: 'Date requise', detail: 'Veuillez renseigner la date et l\'heure souhaitées.' });
      return;
    }

    const dateSelectionnee = new Date(this.nouvelleDemande.dateSouhaitee);
    if (dateSelectionnee < new Date()) {
      this.messageService.add({ severity: 'warn', summary: 'Date invalide', detail: 'La date et l\'heure ne peuvent pas être antérieures à la date du jour.' });
      return;
    }

    if (this.isEditMode && this.nouvelleDemande.id) {
      this.demandesService.updateDemandeVisite(this.nouvelleDemande.id, this.nouvelleDemande).subscribe({
        next: () => {
          this.displayModal = false;
          this.messageService.add({ severity: 'success', summary: 'Succès', detail: 'Demande mise à jour avec succès.' });
          this.chargerDemandes();
        },
        error: (err) => {
          console.error('Erreur mise à jour :', err);
          this.messageService.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de modifier la demande.' });
        }
      });
    } else {
      const payload = {
        bienId: this.nouvelleDemande.bienId,
        agentId: this.nouvelleDemande.agentId || null,
        nomProspect: this.nouvelleDemande.nomProspect,
        telephoneProspect: this.nouvelleDemande.telephoneProspect,
        dateSouhaitee: this.nouvelleDemande.dateSouhaitee
      };

      this.demandesService.createDemandeVisite(payload).subscribe({
        next: (demandeCreee) => {
          this.demandes.unshift(demandeCreee);
          this.displayModal = false;
          this.messageService.add({ severity: 'success', summary: 'Succès', detail: 'Demande créée avec succès.' });
          this.chargerDemandes();
        },
        error: (err) => {
          console.error('Erreur création :', err);
          this.messageService.add({ severity: 'error', summary: 'Erreur', detail: 'Erreur lors de la création.' });
        }
      });
    }
  }

  changerStatut(demande: DemandeVisiteDto, nouveauStatut: StatutDemandeVisite): void {
    if (!demande.id) return;
    const demandeMiseAJour: DemandeVisiteDto = { ...demande, statut: nouveauStatut };
    this.demandesService.updateDemandeVisite(demande.id, demandeMiseAJour).subscribe({
      next: () => {
        demande.statut = nouveauStatut;
        this.messageService.add({ severity: 'success', summary: 'Succès', detail: 'Statut mis à jour.' });
      },
      error: (err) => {
        console.error('Erreur :', err);
        this.messageService.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de modifier le statut.' });
      }
    });
  }

  supprimerDemande(id?: string): void {
    if (!id) return;
    this.confirmationService.confirm({
      message: 'Êtes-vous sûr de vouloir supprimer cette demande de visite ?',
      header: 'Confirmation',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.demandesService.deleteDemandeVisite(id).subscribe({
          next: () => {
            this.demandes = this.demandes.filter(d => d.id !== id);
            this.messageService.add({ severity: 'success', summary: 'Supprimé', detail: 'Demande supprimée.' });
          },
          error: (err) => {
            console.error('Erreur :', err);
            this.messageService.add({ severity: 'error', summary: 'Erreur', detail: 'Suppression impossible.' });
          }
        });
      }
    });
  }

  statutsDisponibles = [
  { label: 'En attente', value: StatutDemandeVisite.EnAttente },
  { label: 'Confirmée', value: StatutDemandeVisite.Confirmee },
  { label: 'Annulée', value: StatutDemandeVisite.Annulee },
  { label: 'Effectuée', value: StatutDemandeVisite.Effectuee }
];

  getStatutLibelle(statut?: number): string {
    switch (statut) {
      case StatutDemandeVisite.EnAttente: return 'En attente';
      case StatutDemandeVisite.Confirmee: return 'Confirmée';
      case StatutDemandeVisite.Annulee: return 'Annulée';
      case StatutDemandeVisite.Effectuee: return 'Effectuée';
      default: return 'Inconnu';
    }
  }

  getStatutBadgeClass(statut?: number): string {
    switch (statut) {
      case StatutDemandeVisite.EnAttente: return 'bg-yellow-100';
      case StatutDemandeVisite.Confirmee: return 'bg-green-100';
      case StatutDemandeVisite.Annulee: return 'bg-red-100';
      case StatutDemandeVisite.Effectuee: return 'bg-blue-100';
      default: return 'bg-gray-100 text-gray-700';
    }
  }
}