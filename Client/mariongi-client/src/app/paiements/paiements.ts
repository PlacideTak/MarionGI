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
  StatutTransaction
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

  filtreStatut?: StatutTransaction;

  filtreMode?: ModePaiement;


  // =========================================================
  // ENUMS EXPOSÉS AU TEMPLATE
  // =========================================================

  readonly StatutTransaction =
    StatutTransaction;

  readonly ModePaiement =
    ModePaiement;


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

      this.contratId =
        params.get('contratId');

      this.modeContrat =
        !!this.contratId;

      this.chargerPaiements();
    });
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

          this.paiements =
            data ?? [];


          // ---------------------------------------------------
          // FILTRAGE PAR CONTRAT
          // ---------------------------------------------------

          if (this.contratId) {

            this.paiements =
              this.paiements.filter(
                p =>
                  p.contratId === this.contratId
              );
          }


          this.paiementsFiltres =
            [...this.paiements];

          this.chargementEnCours =
            false;
        },

        error: (err: unknown) => {

          console.error(
            'Erreur lors du chargement des paiements :',
            err
          );

          this.paiements = [];

          this.paiementsFiltres = [];

          this.chargementEnCours =
            false;
        }
      });
  }


  // =========================================================
  // FILTRER LES PAIEMENTS
  // =========================================================

  appliquerFiltres(): void {

    const texte =
      this.filtreTexte
        .trim()
        .toLowerCase();


    this.paiementsFiltres =
      this.paiements.filter(p => {

        // -----------------------------------------------------
        // RECHERCHE TEXTE
        // -----------------------------------------------------

        const matchTexte =

          !texte ||

          (
            p.numeroQuittance
              ?.toLowerCase()
              .includes(texte)
            ?? false
          ) ||

          (
            p.bienReference
              ?.toLowerCase()
              .includes(texte)
            ?? false
          ) ||

          (
            p.bienNom
              ?.toLowerCase()
              .includes(texte)
            ?? false
          );


        // -----------------------------------------------------
        // FILTRE STATUT
        // -----------------------------------------------------

        const matchStatut =

          this.filtreStatut == null ||

          p.statut ===
            this.filtreStatut;


        // -----------------------------------------------------
        // FILTRE MODE
        // -----------------------------------------------------

        const matchMode =

          this.filtreMode == null ||

          p.mode ===
            this.filtreMode;


        return (
          matchTexte &&
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

    this.filtreStatut =
      undefined;

    this.filtreMode =
      undefined;

    this.paiementsFiltres =
      [...this.paiements];
  }


  // =========================================================
  // TÉLÉCHARGER LA QUITTANCE
  // =========================================================

  telechargerQuittance(
    paiementId: string
  ): void {

    this.paiementsService
      .telechargerQuittanceBlob(
        paiementId
      );
  }


  // =========================================================
  // COULEUR DU STATUT
  // =========================================================

  getSeverity(
    statut: StatutTransaction
  ):
    'success'
    | 'warn'
    | 'danger'
    | 'info' {

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

    const found =
      this.modesOptions.find(
        m => m.value === mode
      );

    return found?.label ?? 'Autre';
  }
}