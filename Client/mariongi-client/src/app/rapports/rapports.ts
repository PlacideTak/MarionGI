import {
  ChangeDetectorRef,
  Component,
  OnInit,
  inject,
  signal
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { MessageService } from 'primeng/api';

// ============================================================
// IMPORTS PRIMENG
// ============================================================

import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DialogModule } from 'primeng/dialog';
import { TableModule } from 'primeng/table';
import { SelectModule } from 'primeng/select';
import { ToastModule } from 'primeng/toast';

// ============================================================
// SERVICES
// ============================================================

import { RapportsService } from '../services/rapports.service';
import { UtilisateursService } from '../services/utilisateurs.service';

// ============================================================
// MODELS
// ============================================================

import {
  UtilisateurDto,
  RoleUtilisateur
} from '../models/gestimmo.models';


// ============================================================
// COMPOSANT
// ============================================================

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

  providers: [
    MessageService
  ],

  templateUrl: './rapports.html',
  styleUrls: ['./rapports.scss']
})
export class Rapports implements OnInit {

  // ==========================================================
  // SERVICES
  // ==========================================================

  private readonly rapportsService = inject(RapportsService);

  private readonly messageService = inject(MessageService);

  private readonly utilisateurService =
    inject(UtilisateursService);

  private readonly cdr =
    inject(ChangeDetectorRef);


  // ==========================================================
  // ÉTAT
  // ==========================================================

  readonly isLoading = signal<boolean>(false);

  /**
   * Titre affiché au-dessus du rapport.
   */
  rapportActifTitre = '';

  /**
   * Type du rapport actuellement affiché.
   *
   * Exemples :
   * - encaissements
   * - historique-locataire
   * - contrats
   * - impayes
   */
  typeRapportActif = '';


  /**
   * Données effectivement utilisées par les tableaux.
   */
  donneesRapport: any[] = [];


  /**
   * Données complètes retournées par l'API.
   *
   * Utilisées notamment pour l'export PDF.
   *
   * Exemple :
   *
   * historique-locataire :
   * {
   *   locataire: {...},
   *   totalVersements: ...,
   *   historique: [...]
   * }
   */
  donneesBrutesGlobales: any = null;


  // ==========================================================
  // LOCATAIRES
  // ==========================================================

  locataires: {
    label: string;
    value: string;
  }[] = [];

  LocataireId: string | null = null;


  // ==========================================================
  // MODALE PARAMÈTRES
  // ==========================================================

  displayModalParametres = false;

  titreModalParametres = '';

  rapportCibleParametres = '';

  filtreLocataire = '';

  paramDateDebut?: string;

  paramDateFin?: string;


  // ==========================================================
  // INITIALISATION
  // ==========================================================

  ngOnInit(): void {

    /**
     * Les locataires sont chargés dès l'ouverture
     * de la page afin qu'ils soient disponibles
     * lorsque l'utilisateur sélectionne le rapport
     * "Historique locataire".
     */
    this.chargerLocataires();
  }


  // ==========================================================
  // CHARGEMENT DES LOCATAIRES
  // ==========================================================

  chargerLocataires(): void {

    this.utilisateurService.getUtilisateurs().subscribe({

      next: (data: UtilisateurDto[]) => {

        this.locataires = data
          .filter(
            (u: UtilisateurDto) =>
              u.role === RoleUtilisateur.Locataire
          )
          .map(
            (u: UtilisateurDto) => ({
              label: `${u.prenom} ${u.nom} (${u.email})`,
              value: u.id
            })
          );

        this.cdr.detectChanges();
      },

      error: (err: unknown) => {

        console.error(
          'Erreur chargement locataires :',
          err
        );

        this.locataires = [];

        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: 'Impossible de charger la liste des locataires.'
        });

        this.cdr.detectChanges();
      }
    });
  }


  // ==========================================================
  // FILTRE DES LOCATAIRES
  // ==========================================================

  get locatairesFiltres(): {
    label: string;
    value: string;
  }[] {

    if (
      !this.filtreLocataire ||
      this.filtreLocataire.trim() === ''
    ) {
      return this.locataires;
    }

    const recherche =
      this.filtreLocataire
        .trim()
        .toLowerCase();

    return this.locataires.filter(
      loc =>
        loc.label
          .toLowerCase()
          .includes(recherche)
    );
  }


  // ==========================================================
  // ROUTAGE DES RAPPORTS
  // ==========================================================

  executerRapport(type: string): void {

    // --------------------------------------------------------
    // Réinitialisation
    // --------------------------------------------------------

    this.typeRapportActif = type;

    this.rapportActifTitre = '';

    this.donneesRapport = [];

    this.donneesBrutesGlobales = null;


    // --------------------------------------------------------
    // Rapport encaissements
    // --------------------------------------------------------

    switch (type) {

      case 'encaissements':

        this.titreModalParametres =
          'Choix de la période';

        this.rapportCibleParametres =
          'encaissements';

        this.paramDateDebut =
          undefined;

        this.paramDateFin =
          undefined;

        this.displayModalParametres =
          true;

        break;


      // ------------------------------------------------------
      // Historique locataire
      // ------------------------------------------------------

      case 'historique-locataire':

        this.titreModalParametres =
          'Paiements d’un locataire';

        this.rapportCibleParametres =
          'historique-locataire';

        this.LocataireId =
          null;

        this.filtreLocataire =
          '';

        this.displayModalParametres =
          true;

        break;


      // ------------------------------------------------------
      // Contrats
      // ------------------------------------------------------

      case 'contrats':

        this.chargerRapportContrats();

        break;


      // ------------------------------------------------------
      // Impayés
      // ------------------------------------------------------

      case 'impayes':

        this.chargerRapportImpayes();

        break;


      // ------------------------------------------------------
      // Rapport inconnu
      // ------------------------------------------------------

      default:

        console.warn(
          'Type de rapport inconnu :',
          type
        );

        break;
    }
  }


  // ==========================================================
  // VALIDATION DE LA MODALE
  // ==========================================================

  validerParametresModal(): void {

    // ========================================================
    // 1. ENCAISSEMENTS
    // ========================================================

    if (
      this.rapportCibleParametres ===
      'encaissements'
    ) {

      // ------------------------------------------------------
      // Vérification des dates
      // ------------------------------------------------------

      if (
        !this.paramDateDebut ||
        !this.paramDateFin
      ) {

        this.messageService.add({
          severity: 'warn',
          summary: 'Champs requis',
          detail:
            'Veuillez renseigner la date de début et la date de fin.'
        });

        return;
      }


      // ------------------------------------------------------
      // Validation du format
      // ------------------------------------------------------

      const dateRegex =
        /^\d{4}-\d{2}-\d{2}$/;

      if (
        !dateRegex.test(this.paramDateDebut) ||
        !dateRegex.test(this.paramDateFin)
      ) {

        this.messageService.add({
          severity: 'warn',
          summary: 'Format invalide',
          detail:
            'Les dates doivent être au format AAAA-MM-JJ.'
        });

        return;
      }


      // ------------------------------------------------------
      // Comparaison des dates
      // ------------------------------------------------------

      if (
        this.paramDateDebut >
        this.paramDateFin
      ) {

        this.messageService.add({
          severity: 'warn',
          summary: 'Dates incohérentes',
          detail:
            'La date de début ne peut pas être supérieure à la date de fin.'
        });

        return;
      }


      // ------------------------------------------------------
      // Fermeture de la modale
      // ------------------------------------------------------

      this.displayModalParametres =
        false;

      this.isLoading.set(true);


      // IMPORTANT :
      //
      // On transmet directement YYYY-MM-DD.
      //
      // On évite :
      //
      // new Date(...).toISOString()
      //
      // qui peut provoquer un décalage de date
      // à cause du fuseau horaire.
      //

      const dateDebut =
        this.paramDateDebut;

      const dateFin =
        this.paramDateFin;


      // ------------------------------------------------------
      // Appel API
      // ------------------------------------------------------

      this.rapportsService
        .getEncaissementsParMode(
          dateDebut,
          dateFin
        )
        .subscribe({

          next: (data) => {

            this.donneesBrutesGlobales =
              data;

            /**
             * Les détails sont affichés
             * dans le tableau.
             *
             * La réponse API est :
             *
             * {
             *   total,
             *   nombreTransactions,
             *   parMode: [...]
             * }
             *
             * On conserve cependant l'objet complet
             * dans donneesBrutesGlobales pour le PDF.
             */
            this.donneesRapport =
              data?.parMode ?? [];

            this.rapportActifTitre =
              'Encaissements par mode de paiement';


            this.isLoading.set(false);

            this.messageService.add({
              severity: 'success',
              summary: 'Rapport généré',
              detail:
                'Le rapport des encaissements a été chargé avec succès.'
            });
          },

          error: (err: unknown) => {

            console.error(
              'Erreur encaissements :',
              err
            );

            this.isLoading.set(false);

            this.messageService.add({
              severity: 'error',
              summary: 'Erreur',
              detail:
                'Impossible de charger le rapport des encaissements.'
            });
          }
        });

      return;
    }


    // ========================================================
    // 2. HISTORIQUE LOCATAIRE
    // ========================================================

    if (
      this.rapportCibleParametres ===
      'historique-locataire'
    ) {

      // ------------------------------------------------------
      // Vérification du locataire
      // ------------------------------------------------------

      if (
        !this.LocataireId ||
        this.LocataireId.trim() === ''
      ) {

        this.messageService.add({
          severity: 'warn',
          summary: 'Locataire requis',
          detail:
            'Veuillez sélectionner un locataire avant de générer le rapport.'
        });

        return;
      }


      // ------------------------------------------------------
      // Fermeture + chargement
      // ------------------------------------------------------

      this.displayModalParametres =
        false;

      this.isLoading.set(true);


      this.rapportsService
        .getHistoriquePaiementsLocataire(
          this.LocataireId
        )
        .subscribe({

          next: (data) => {

            // ------------------------------------------------
            // Conservation de la réponse complète
            // ------------------------------------------------

            this.donneesBrutesGlobales =
              data;


            // ------------------------------------------------
            // Données du tableau
            // ------------------------------------------------

            this.donneesRapport =
              data?.historique ?? [];


            // ------------------------------------------------
            // Nom du locataire
            // ------------------------------------------------

            const nomLocataire =
              [
                data?.locataire?.prenom,
                data?.locataire?.nom
              ]
                .filter(Boolean)
                .join(' ');


            this.rapportActifTitre =
              `Historique des paiements - ${
                nomLocataire || 'Locataire'
              }`;


            this.isLoading.set(false);

            this.messageService.add({
              severity: 'success',
              summary: 'Rapport généré',
              detail:
                'L’historique du locataire a été chargé avec succès.'
            });
          },

          error: (err: unknown) => {

            console.error(
              'Erreur historique locataire :',
              err
            );

            this.isLoading.set(false);

            this.messageService.add({
              severity: 'error',
              summary: 'Erreur',
              detail:
                'Impossible de charger l’historique du locataire.'
            });
          }
        });

      return;
    }
  }


  // ==========================================================
  // RAPPORT DES CONTRATS
  // ==========================================================

  chargerRapportContrats(): void {

    this.isLoading.set(true);

    this.rapportsService
      .getRapportContrats()
      .subscribe({

        next: (data) => {

          this.donneesRapport =
            data ?? [];

          this.donneesBrutesGlobales =
            data ?? [];

          this.rapportActifTitre =
            'Tableau de tous les contrats';


          this.isLoading.set(false);

          this.messageService.add({
            severity: 'success',
            summary: 'Rapport généré',
            detail:
              'La liste des contrats a été chargée avec succès.'
          });
        },

        error: (err: unknown) => {

          console.error(
            'Erreur rapport contrats :',
            err
          );

          this.isLoading.set(false);

          this.messageService.add({
            severity: 'error',
            summary: 'Erreur',
            detail:
              'Impossible de charger le rapport des contrats.'
          });
        }
      });
  }


  // ==========================================================
  // RAPPORT DES IMPAYÉS
  // ==========================================================

  chargerRapportImpayes(): void {

    this.isLoading.set(true);

    this.rapportsService
      .getRapportImpayes()
      .subscribe({

        next: (data) => {

          /**
           * IMPORTANT :
           *
           * L'API retourne maintenant :
           *
           * {
           *   nombreImpayes,
           *   montantTotalImpayes,
           *   impayes: [...]
           * }
           *
           * Le tableau doit donc recevoir :
           *
           * data.impayes
           *
           * et non data directement.
           */

          this.donneesRapport =
            data?.impayes ?? [];


          /**
           * On conserve la réponse complète
           * pour l'export PDF.
           */
          this.donneesBrutesGlobales =
            data;


          this.rapportActifTitre =
            'Liste des impayés après expiration du délai de tolérance';


          this.isLoading.set(false);

          this.messageService.add({
            severity: 'success',
            summary: 'Rapport généré',
            detail:
              'Le rapport des impayés a été chargé avec succès.'
          });
        },

        error: (err: unknown) => {

          console.error(
            'Erreur rapport impayés :',
            err
          );

          this.isLoading.set(false);

          this.messageService.add({
            severity: 'error',
            summary: 'Erreur',
            detail:
              'Impossible de charger le rapport des impayés.'
          });
        }
      });
  }


  // ==========================================================
  // EXPORT PDF
  // ==========================================================

  telechargerPdf(): void {

    // --------------------------------------------------------
    // Vérification du rapport
    // --------------------------------------------------------

    if (
      !this.typeRapportActif
    ) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Attention',
        detail:
          'Veuillez d’abord générer un rapport.'
      });

      return;
    }


    // --------------------------------------------------------
    // Sélection des données à envoyer
    // --------------------------------------------------------

    const payload =
      this.donneesBrutesGlobales ??
      this.donneesRapport;


    if (
      payload === null ||
      payload === undefined
    ) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Attention',
        detail:
          'Aucune donnée à exporter.'
      });

      return;
    }


    // --------------------------------------------------------
    // Chargement
    // --------------------------------------------------------

    this.isLoading.set(true);


    // --------------------------------------------------------
    // Appel API PDF
    // --------------------------------------------------------

    this.rapportsService
      .telechargerRapportPdf(
        this.typeRapportActif,
        this.rapportActifTitre,
        payload
      )
      .subscribe({

        next: (blob: Blob) => {

          // --------------------------------------------------
          // Création du téléchargement
          // --------------------------------------------------

          const url =
            window.URL.createObjectURL(blob);

          const link =
            document.createElement('a');

          link.href = url;

          link.download =
            `Rapport_${this.typeRapportActif}_${this.getDateFichier()}.pdf`;


          document.body.appendChild(link);

          link.click();

          document.body.removeChild(link);

          window.URL.revokeObjectURL(url);


          this.isLoading.set(false);

          this.messageService.add({
            severity: 'success',
            summary: 'Export terminé',
            detail:
              'Le rapport PDF a été généré avec succès.'
          });
        },

        error: (err: unknown) => {

          console.error(
            'Erreur téléchargement PDF :',
            err
          );

          this.isLoading.set(false);

          this.messageService.add({
            severity: 'error',
            summary: 'Erreur',
            detail:
              'Impossible de générer le fichier PDF.'
          });
        }
      });
  }


  // ==========================================================
  // DATE POUR LE NOM DU FICHIER
  // ==========================================================

  private getDateFichier(): string {

    const maintenant =
      new Date();

    const annee =
      maintenant.getFullYear();

    const mois =
      String(
        maintenant.getMonth() + 1
      ).padStart(2, '0');

    const jour =
      String(
        maintenant.getDate()
      ).padStart(2, '0');

    return `${annee}-${mois}-${jour}`;
  }
}