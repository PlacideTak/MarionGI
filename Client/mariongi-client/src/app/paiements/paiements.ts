import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';

import { PaiementDto, ModePaiement, StatutTransaction } from '../models/gestimmo.models';
import { PaiementsService } from '../services/paiements.service';

@Component({
  selector: 'app-paiements-list',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    FormsModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    SelectModule,
    TagModule,
    TooltipModule
  ],
  templateUrl: './paiements.html',
  styleUrls: ['./paiements.scss']
})
export class Paiements implements OnInit {
  private readonly paiementsService = inject(PaiementsService);

  paiements: PaiementDto[] = [];
  paiementsFiltres: PaiementDto[] = [];
  chargementEnCours: boolean = true;

  // Filtres
  filtreTexte: string = '';
  filtreStatut?: StatutTransaction;
  filtreMode?: ModePaiement;

  // Enums exposés pour le template
  readonly StatutTransaction = StatutTransaction;
  readonly ModePaiement = ModePaiement;

  readonly statutsOptions = [
    { label: 'En attente', value: StatutTransaction.EnAttente },
    { label: 'Confirmé', value: StatutTransaction.Confirme },
    { label: 'Échoué', value: StatutTransaction.Echoue }
  ];

  readonly modesOptions = [
    { label: 'Orange Money', value: ModePaiement.OrangeMoney },
    { label: 'MTN Money', value: ModePaiement.MtnMoney },
    { label: 'Espèces', value: ModePaiement.Especes },
    { label: 'M2U', value: ModePaiement.M2u },
    { label: 'SARAH Money', value: ModePaiement.SaraMoney }
  ];

  ngOnInit(): void {
    this.chargerPaiements();
  }

  chargerPaiements(): void {
    this.chargementEnCours = true;
    this.paiementsService.getPaiements().subscribe({
      next: (data) => {
        this.paiements = data;
        this.paiementsFiltres = [...this.paiements]; // Initialisation de la liste filtrée
        this.chargementEnCours = false;
      },
      error: (err) => {
        console.error('Erreur lors du chargement des paiements :', err);
        this.chargementEnCours = false;
      }
    });
  }

  appliquerFiltres(): void {
    this.paiementsFiltres = this.paiements.filter(p => {
      const matchTexte = !this.filtreTexte || 
        (p.numeroQuittance && p.numeroQuittance.toLowerCase().includes(this.filtreTexte.toLowerCase())) ||
        ((p as any).bienReference && (p as any).bienReference.toLowerCase().includes(this.filtreTexte.toLowerCase()));
        
      const matchStatut = this.filtreStatut === undefined || this.filtreStatut === null || p.statut === this.filtreStatut;
      const matchMode = this.filtreMode === undefined || this.filtreMode === null || p.mode === this.filtreMode;

      return matchTexte && matchStatut && matchMode;
    });
  }

  telechargerQuittance(paiementId: string): void {
    this.paiementsService.telechargerQuittanceBlob(paiementId);
  }

  getSeverity(statut: StatutTransaction): 'success' | 'warn' | 'danger' | 'info' {
    switch (statut) {
      case StatutTransaction.Confirme: return 'success';
      case StatutTransaction.EnAttente: return 'warn';
      case StatutTransaction.Echoue: return 'danger';
      default: return 'info';
    }
  }

  getLibelleStatut(statut: StatutTransaction): string {
    switch (statut) {
      case StatutTransaction.Confirme: return 'Confirmé';
      case StatutTransaction.EnAttente: return 'En attente';
      case StatutTransaction.Echoue: return 'Échoué';
      default: return 'Inconnu';
    }
  }

  getLibelleMode(mode: ModePaiement): string {
    const found = this.modesOptions.find(m => m.value === mode);
    return found ? found.label : 'Autre';
  }
}