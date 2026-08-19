import { ChangeDetectorRef, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';

// Imports PrimeNG
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DialogModule } from 'primeng/dialog';
import { TableModule } from 'primeng/table';
import { SelectModule } from 'primeng/select';

// Services
import { RapportsService } from '../services/rapports.service';
import { UtilisateursService } from '../services/utilisateurs.service';

// Models
import { UtilisateurDto, RoleUtilisateur } from '../models/gestimmo.models';
import { ToastModule } from "primeng/toast";

@Component({
  selector: 'app-rapports',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    CardModule,
    DialogModule,
    TableModule,
    SelectModule,
    ToastModule
],
  providers: [MessageService],
  templateUrl: './rapports.html',
  styleUrls: ['./rapports.scss']
})
export class Rapports implements OnInit {

  // =====================================================
  // SERVICES
  // =====================================================

  private readonly rapportsService = inject(RapportsService);
  private readonly messageService = inject(MessageService);
  private readonly utilisateurService = inject(UtilisateursService);
  private readonly cdr = inject(ChangeDetectorRef);

  // =====================================================
  // ÉTAT
  // =====================================================

  readonly isLoading = signal<boolean>(false);

  rapportActifTitre: string = '';
  typeRapportActif: string = '';

  donneesRapport: any[] = [];
  donneesBrutesGlobales: any = null;

  locataires: { label: string; value: string }[] = [];
  LocataireId: string | null = null;

  displayModalParametres: boolean = false;
  titreModalParametres: string = '';
  rapportCibleParametres: string = '';
  filtreLocataire: string = '';
  paramDateDebut?: string;
  paramDateFin?: string;

  ngOnInit(): void {
    // ⚡ CHARGEMENT ANTICIPÉ : Les locataires sont prêts dès l'ouverture de la page
    this.chargerLocataires();
  }

  // =====================================================
  // CHARGEMENT DES LOCATAIRES
  // =====================================================

chargerLocataires(): void {
    this.utilisateurService.getUtilisateurs().subscribe({
      next: (data: UtilisateurDto[]) => {
        this.locataires = data
          .filter((u: UtilisateurDto) => u.role === RoleUtilisateur.Locataire)
          .map((u: UtilisateurDto) => ({
            label: `${u.prenom} ${u.nom} (${u.email})`,
            value: u.id
          }));

        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Erreur chargement locataires', err);
        this.locataires = [];
        this.cdr.detectChanges();
      }
    });
  }

  get locatairesFiltres() {
    if (!this.filtreLocataire || this.filtreLocataire.trim() === '') {
      return this.locataires;
    }
    const recherche = this.filtreLocataire.toLowerCase();
    return this.locataires.filter(loc => loc.label.toLowerCase().includes(recherche));
  }

  // =====================================================
  // ROUTAGE DES RAPPORTS
  // =====================================================

  executerRapport(type: string): void {
    this.typeRapportActif = type;
    this.rapportActifTitre = '';
    this.donneesRapport = [];
    this.donneesBrutesGlobales = null;

    switch (type) {
      case 'encaissements':
        this.titreModalParametres = 'Choix de la période';
        this.rapportCibleParametres = type;
        this.paramDateDebut = undefined;
        this.paramDateFin = undefined;
        this.displayModalParametres = true;
        break;

      case 'historique-locataire':
        this.titreModalParametres = 'Paiements d\'un locataire';
        this.rapportCibleParametres = type;
        this.LocataireId = null;
        this.filtreLocataire = '';
        this.displayModalParametres = true;
        break;

      case 'contrats':
        this.chargerRapportContrats();
        break;

      case 'impayes':
        this.chargerRapportImpayes();
        break;

      default:
        console.warn('Type de rapport inconnu :', type);
        break;
    }
  }

  // =====================================================
  // VALIDATION DES PARAMÈTRES DE LA MODALE
  // =====================================================

validerParametresModal(): void {
    // ==========================================
    // 1. VALIDATION RAPPORT ENCAISSEMENTS
    // ==========================================
    if (this.rapportCibleParametres === 'encaissements') {
      if (!this.paramDateDebut || this.paramDateDebut.trim() === '' || 
          !this.paramDateFin || this.paramDateFin.trim() === '') {
        this.messageService.add({
          severity: 'warn',
          summary: 'Champs requis',
          detail: 'Veuillez renseigner à la fois la date de début et la date de fin.'
        });
        return;
      }

      const dateRegex = /^\d{4}-\d{2}-\d{2}$/;
      if (!dateRegex.test(this.paramDateDebut) || !dateRegex.test(this.paramDateFin)) {
        this.messageService.add({
          severity: 'warn',
          summary: 'Format invalide',
          detail: 'Veuillez saisir une date complète valide (Jour, Mois, Année).'
        });
        return;
      }

      const dateDebutObj = new Date(this.paramDateDebut);
      const dateFinObj = new Date(this.paramDateFin);

      if (dateDebutObj > dateFinObj) {
        this.messageService.add({
          severity: 'warn',
          summary: 'Dates incohérentes',
          detail: 'La date de début ne peut pas être supérieure à la date de fin.'
        });
        return;
      }

      this.displayModalParametres = false;

      const dDebut = new Date(this.paramDateDebut).toISOString();
      const dFin = new Date(this.paramDateFin).toISOString();

      this.rapportsService.getEncaissementsParMode(dDebut, dFin).subscribe({
        next: (data) => {
          this.donneesBrutesGlobales = data;
          this.rapportActifTitre = 'Liste des encaissements périodiques regroupés par mode de paiement';
          this.messageService.add({
            severity: 'success',
            summary: 'Rapport généré',
            detail: 'Le rapport a été chargé avec succès.'
          });
        },
        error: (err) => {
          console.error('Erreur encaissements :', err);
          this.messageService.add({
            severity: 'error',
            summary: 'Erreur',
            detail: 'Impossible de charger le rapport des encaissements.'
          });
        }
      });
      return;
    }

    // ==========================================
    // 2. VALIDATION RAPPORT HISTORIQUE LOCATAIRE
    // ==========================================
    if (this.rapportCibleParametres === 'historique-locataire') {
      // ⚡ Vérification stricte si aucun locataire n'est sélectionné (null, undefined ou chaîne vide)
      if (!this.LocataireId || this.LocataireId === '' || (typeof this.LocataireId === 'string' && this.LocataireId.trim() === '')) {
        this.messageService.add({
          severity: 'warn',
          summary: 'Locataire requis',
          detail: 'Veuillez sélectionner un locataire dans la liste avant de générer le rapport.'
        });
        return; // Stoppe net l'exécution
      }

      this.displayModalParametres = false;
      this.rapportsService.getHistoriquePaiementsLocataire(this.LocataireId).subscribe({
        next: (data) => {
          this.donneesBrutesGlobales = data;
          this.donneesRapport = data.historique || [];
          this.rapportActifTitre = `Historique des paiements - ${[data.locataire?.prenom, data.locataire?.nom].filter(Boolean).join(' ') || 'Locataire'}`;
          this.messageService.add({
            severity: 'success',
            summary: 'Rapport généré',
            detail: 'Historique chargé avec succès.'
          });
        },
        error: (err) => {
          console.error('Erreur historique locataire :', err);
          this.messageService.add({
            severity: 'error',
            summary: 'Erreur',
            detail: 'Impossible de charger l\'historique du locataire.'
          });
        }
      });
      return;
    }
  }

  chargerRapportContrats(): void {
    this.isLoading.set(true);
    this.rapportsService.getRapportContrats().subscribe({
      next: (data) => {
        this.donneesRapport = data;
        this.rapportActifTitre = 'Tableau de tous les contrats périodiques';
        this.isLoading.set(false);
        this.messageService.add({ severity: 'success', summary: 'Rapport généré', detail: 'Liste des contrats chargée avec succès.' });
      },
      error: (err) => {
        console.error('Erreur rapport contrats :', err);
        this.isLoading.set(false);
        this.messageService.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de charger le rapport des contrats.' });
      }
    });
  }

  chargerRapportImpayes(): void {
    this.isLoading.set(true);
    this.rapportsService.getRapportImpayes().subscribe({
      next: (data) => {
        this.donneesRapport = data;
        this.rapportActifTitre = 'Liste des biens dont le loyer n\'a pas été payé après le délai de tolérance';
        this.isLoading.set(false);
        this.messageService.add({ severity: 'success', summary: 'Rapport généré', detail: 'Rapport des impayés chargé avec succès.' });
      },
      error: (err) => {
        console.error('Erreur rapport impayés :', err);
        this.isLoading.set(false);
        this.messageService.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de charger le rapport des impayés.' });
      }
    });
  }

 telechargerPdf(): void {
    if (!this.typeRapportActif) return;

    // Pour l'historique locataire ET les encaissements, 
    // on envoie les données brutes globales qui contiennent toute la structure nécessaire
    const payload = (this.typeRapportActif === 'encaissements' || this.typeRapportActif === 'historique-locataire') 
                    ? this.donneesBrutesGlobales 
                    : this.donneesRapport;

    if (!payload) {
        this.messageService.add({ severity: 'warn', summary: 'Attention', detail: 'Aucune donnée à exporter.' });
        return;
    }

    this.isLoading.set(true);
    this.rapportsService.telechargerRapportPdf(this.typeRapportActif, this.rapportActifTitre, payload).subscribe({
        next: (blob) => {
            const url = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = `Rapport_${this.typeRapportActif}_${new Date().toISOString().slice(0, 10)}.pdf`;
            link.click();
            window.URL.revokeObjectURL(url);
            
            this.isLoading.set(false);
        },
        error: (err) => {
            console.error('Erreur téléchargement PDF', err);
            this.isLoading.set(false);
            this.messageService.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de générer le fichier PDF.' });
        }
    });
}
}