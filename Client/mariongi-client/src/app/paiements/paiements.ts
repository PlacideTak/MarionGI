
import {
  Component,
  OnInit,
  inject
} from '@angular/core';

import {
  CommonModule,
  DatePipe
} from '@angular/common';

import {
  FormsModule
} from '@angular/forms';

import {
  ActivatedRoute
} from '@angular/router';

import {
  TableModule
} from 'primeng/table';

import {
  ButtonModule
} from 'primeng/button';

import {
  InputTextModule
} from 'primeng/inputtext';

import {
  SelectModule
} from 'primeng/select';

import {
  TagModule
} from 'primeng/tag';

import {
  TooltipModule
} from 'primeng/tooltip';

import {
  PaiementListItem,
  ModePaiement,
  StatutTransaction,
  TypePaiement
} from '../models/gestimmo.models';

import {
  PaiementsService
} from '../services/paiements.service';

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

  // =========================================================
  // SERVICES
  // =========================================================

  private readonly paiementsService =
    inject(PaiementsService);

  private readonly route =
    inject(ActivatedRoute);

  // =========================================================
  // DONNÉES
  // =========================================================

  paiements: PaiementListItem[] = [];

  paiementsFiltres: PaiementListItem[] = [];

  chargementEnCours = true;

  // =========================================================
  // CONTRAT SÉLECTIONNÉ
  // =========================================================

  contratId: string | null = null;

  modeContrat = false;

  // =========================================================
  // FILTRES
  // =========================================================

  filtreTexte = '';

  filtreTypePaiement?: TypePaiement;

  filtreStatut?: StatutTransaction;

  filtreMode?: ModePaiement;

  // =========================================================
  // ENUMS EXPOSÉS AU TEMPLATE
  // =========================================================

  readonly StatutTransaction = StatutTransaction;

  readonly ModePaiement = ModePaiement;

  readonly TypePaiement = TypePaiement;

  // =========================================================
  // OPTIONS TYPE DE PAIEMENT
  // =========================================================

  readonly typesPaiementOptions = [
    {
      label: 'Loyer',
      value: TypePaiement.Loyer
    },
    {
      label: 'Charges locatives',
      value: TypePaiement.ChargesLocatives
    },
    {
      label: 'Caution',
      value: TypePaiement.Caution
    },
    {
      label: 'Autre',
      value: TypePaiement.Autre
    }
  ];

  // =========================================================
  // OPTIONS STATUT
  // =========================================================

  readonly statutsOptions = [
    {
      label: 'En attente',
      value: StatutTransaction.EnAttente
    },
    {
      label: 'Confirmé',
      value: StatutTransaction.Confirme
    },
    {
      label: 'Échoué',
      value: StatutTransaction.Echoue
    }
  ];

  // =========================================================
  // OPTIONS MODE DE PAIEMENT
  // =========================================================

  readonly modesOptions = [
    {
      label: 'Orange Money',
      value: ModePaiement.OrangeMoney
    },
    {
      label: 'MTN Money',
      value: ModePaiement.MtnMoney
    },
    {
      label: 'Espèces',
      value: ModePaiement.Especes
    },
    {
      label: 'M2U',
      value: ModePaiement.M2u
    },
    {
      label: 'SARAH Money',
      value: ModePaiement.SaraMoney
    }
  ];

  // =========================================================
  // INITIALISATION
  // =========================================================

  ngOnInit(): void {

    this.route.queryParamMap.subscribe(params => {

      this.contratId = params.get('contratId');

      this.modeContrat = !!this.contratId;

      this.chargerPaiements();
    });
  }

  // =========================================================
  // STATISTIQUES DES PAIEMENTS
  // =========================================================

  get totalPaiements(): number {

    return this.paiements.length;
  }

  get nombrePaiementsEnAttente(): number {

    return this.paiements.filter(
      p => p.statut === StatutTransaction.EnAttente
    ).length;
  }

  get nombrePaiementsConfirmes(): number {

    return this.paiements.filter(
      p => p.statut === StatutTransaction.Confirme
    ).length;
  }

  get nombrePaiementsEchoues(): number {

    return this.paiements.filter(
      p => p.statut === StatutTransaction.Echoue
    ).length;
  }

  // =========================================================
  // LIBELLÉ DU TYPE DE PAIEMENT
  // =========================================================

  getLibelleTypePaiement(
    type: TypePaiement
  ): string {

    switch (type) {

      case TypePaiement.Loyer:
        return 'Loyer';

      case TypePaiement.ChargesLocatives:
        return 'Charges locatives';

      case TypePaiement.Caution:
        return 'Caution';

      case TypePaiement.Autre:
        return 'Autre';

      default:
        return 'Non défini';
    }
  }

  // =========================================================
  // CLASSE CSS DU TYPE DE PAIEMENT
  // =========================================================

  getClasseTypePaiement(
    type: TypePaiement
  ): string {

    switch (type) {

      case TypePaiement.Loyer:
        return 'paiement-type-loyer';

      case TypePaiement.ChargesLocatives:
        return 'paiement-type-charges';

      case TypePaiement.Caution:
        return 'paiement-type-caution';

      case TypePaiement.Autre:
        return 'paiement-type-autre';

      default:
        return 'paiement-type-inconnu';
    }
  }

  // =========================================================
  // MONTANT DÉTAILLÉ SELON LE TYPE DE PAIEMENT
  // =========================================================

  getMontantDetail(
    paiement: PaiementListItem
  ): number | null {

    switch (paiement.typePaiement) {

      case TypePaiement.Loyer:
        return paiement.montantLoyerEncaisse ?? null;

      case TypePaiement.ChargesLocatives:
        return paiement.montantChargesLocativesEncaisse ?? null;

      default:
        return null;
    }
  }

  // =========================================================
  // CHARGER LES PAIEMENTS
  // =========================================================

  chargerPaiements(): void {

    this.chargementEnCours = true;

    this.paiementsService
      .getPaiements()
      .subscribe({

        next: (data: PaiementListItem[]) => {

          this.paiements = data ?? [];

          // Filtrage par contrat si nécessaire.
          if (this.contratId) {

            this.paiements = this.paiements.filter(
              p => p.contratId === this.contratId
            );
          }

          this.appliquerFiltres();

          this.chargementEnCours = false;
        },

        error: (err: unknown) => {

          console.error(
            'Erreur lors du chargement des paiements :',
            err
          );

          this.paiements = [];

          this.paiementsFiltres = [];

          this.chargementEnCours = false;
        }
      });
  }

  // =========================================================
  // FILTRER LES PAIEMENTS
  // =========================================================

  appliquerFiltres(): void {

    const texte = this.filtreTexte
      .trim()
      .toLowerCase();

    this.paiementsFiltres = this.paiements.filter(p => {

      // Recherche textuelle.
      const matchTexte =

        !texte ||

        (
          p.numeroQuittance
            ?.toLowerCase()
            .includes(texte) ?? false
        ) ||

        (
          p.bienReference
            ?.toLowerCase()
            .includes(texte) ?? false
        ) ||

        (
          p.bienNom
            ?.toLowerCase()
            .includes(texte) ?? false
        ) ||

        (
          p.uniteReference
            ?.toLowerCase()
            .includes(texte) ?? false
        ) ||

        (
          p.locataireNom
            ?.toLowerCase()
            .includes(texte) ?? false
        ) ||

        this.getLibelleTypePaiement(p.typePaiement)
          .toLowerCase()
          .includes(texte);

      // Filtre par type de paiement.
      const matchTypePaiement =

        this.filtreTypePaiement == null ||

        p.typePaiement === this.filtreTypePaiement;

      // Filtre par statut.
      const matchStatut =

        this.filtreStatut == null ||

        p.statut === this.filtreStatut;

      // Filtre par mode de paiement.
      const matchMode =

        this.filtreMode == null ||

        p.mode === this.filtreMode;

      return (
        matchTexte &&
        matchTypePaiement &&
        matchStatut &&
        matchMode
      );
    });
  }

  // =========================================================
  // RÉINITIALISER LES FILTRES
  // =========================================================

  reinitialiserFiltres(): void {

    this.filtreTexte = '';

    this.filtreTypePaiement = undefined;

    this.filtreStatut = undefined;

    this.filtreMode = undefined;

    this.appliquerFiltres();
  }

  // =========================================================
  // TÉLÉCHARGER LA QUITTANCE
  // =========================================================

  telechargerQuittance(paiementId: string): void {

    this.paiementsService
      .telechargerQuittanceBlob(paiementId);
  }

  // =========================================================
  // COULEUR DU STATUT
  // =========================================================

  getSeverity(
    statut: StatutTransaction
  ): 'success' | 'warn' | 'danger' | 'info' {

    switch (statut) {

      case StatutTransaction.Confirme:
        return 'success';

      case StatutTransaction.EnAttente:
        return 'warn';

      case StatutTransaction.Echoue:
        return 'danger';

      default:
        return 'info';
    }
  }

  // =========================================================
  // LIBELLÉ DU STATUT
  // =========================================================

  getLibelleStatut(
    statut: StatutTransaction
  ): string {

    switch (statut) {

      case StatutTransaction.Confirme:
        return 'Confirmé';

      case StatutTransaction.EnAttente:
        return 'En attente';

      case StatutTransaction.Echoue:
        return 'Échoué';

      default:
        return 'Inconnu';
    }
  }

  // =========================================================
  // LIBELLÉ DU MODE DE PAIEMENT
  // =========================================================

  getLibelleMode(
    mode: ModePaiement
  ): string {

    const found = this.modesOptions.find(
      m => m.value === mode
    );

    return found?.label ?? 'Autre';
  }
}
