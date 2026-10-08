import {
  Component,
  inject,
  OnDestroy,
  OnInit
} from '@angular/core';

import { CommonModule } from '@angular/common';

import {
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';

import {
  MessageService,
  ConfirmationService
} from 'primeng/api';

import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';

import { BiensService } from '../services/biens.service';

import {
  UnitesLocativesService
} from '../services/uniteslocatives.service';

import {
  PaiementsService,
  InitierPaiementRequest,
  PaiementEspecesRequest,
  PaiementResponse
} from '../services/paiements.service';

import { AuthService } from '../login/auth.service';

import {
  BienDto,
  TypeUniteLocative,
  StatutBien,
  StatutContrat,
  ModePaiement,
  ROLES,
  UniteLocativeDto,
  UniteLocativeDetailDto,
  CreerUniteLocativeRequest,
  ModifierUniteLocativeRequest,
  ContratUniteDto
} from '../models/gestimmo.models';


interface MoisLoyerOption {
  label: string;
  value: string;
}


@Component({
  selector: 'app-unites-locatives',

  standalone: true,

  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    SelectModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    InputNumberModule,
    ToastModule,
    ConfirmDialogModule,
    TagModule,
    TooltipModule
  ],

  providers: [
    MessageService,
    ConfirmationService
  ],

  templateUrl: './uniteslocatives.html',
  styleUrls: ['./uniteslocatives.scss']
})
export class UnitesLocatives
  implements OnInit, OnDestroy {


  // ============================================================
  // SERVICES
  // ============================================================

  readonly uniteService =
    inject(UnitesLocativesService);

  readonly bienService =
    inject(BiensService);

  private readonly paiementsService =
    inject(PaiementsService);

  private readonly authService =
    inject(AuthService);

  private readonly fb =
    inject(FormBuilder);

  private readonly messageService =
    inject(MessageService);

  private readonly confirmationService =
    inject(ConfirmationService);


  // ============================================================
  // DONNÉES
  // ============================================================

  unites: UniteLocativeDto[] = [];

  biens: BienDto[] = [];

  chargement = false;

  chargementBiens = false;

  userRole = '';


  // ============================================================
  // MODALE UNITÉ
  // ============================================================

  displayModal = false;

  isEditMode = false;

  selectedUniteId: string | null = null;

  uniteForm!: FormGroup;


  // ============================================================
  // PHOTOS
  // ============================================================

  /**
   * Nouvelles photos sélectionnées depuis l'ordinateur.
   */
  fichiersSelectionnes: File[] = [];

  /**
   * URLs temporaires utilisées pour prévisualiser
   * les nouvelles photos.
   */
  previsualisations: string[] = [];

  /**
   * Photos déjà enregistrées sur le serveur et
   * conservées lors d'une modification.
   */
  photosExistantes: string[] = [];


  // ============================================================
  // DÉTAIL
  // ============================================================

  selectedUnite: UniteLocativeDetailDto | null = null;

  displayDetailModal = false;


  // ============================================================
  // PAIEMENT
  // ============================================================

  /**
   * Affiche la popup de paiement.
   */
  displayPaiementModal = false;

  /**
   * Indique qu'un paiement est actuellement envoyé
   * au backend.
   */
  paiementEnCours = false;

  /**
   * Unité concernée par le paiement.
   */
  unitePaiement: UniteLocativeDetailDto | null = null;

  /**
   * Contrat actif concerné par le paiement.
   */
  contratPaiement: ContratUniteDto | null = null;

  /**
   * Montant saisi dans la popup.
   *
   * Par défaut, il correspond au loyer du contrat actif.
   */
  montantPaiement = 0;

  /**
   * Mois de loyer sélectionné pour le paiement.
   *
   * Format envoyé au backend :
   * YYYY-MM-01T00:00:00
   *
   * Exemple :
   * 2026-10-01T00:00:00
   */
  moisLoyerPaiement = '';

  /**
   * Liste des mois de loyer couverts par le contrat actif.
   */
  moisLoyerOptions: MoisLoyerOption[] = [];

  /**
   * Mode de paiement sélectionné.
   */
  modePaiementSelectionne: ModePaiement | null = null;

  /**
   * Téléphone utilisé pour les paiements électroniques.
   */
  telephonePaiement = '';

  /**
   * Enum exposé au template.
   */
  readonly ModePaiement = ModePaiement;

  /**
   * Liste des modes de paiement disponibles.
   */
  readonly modesPaiement = [
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


  // ============================================================
  // DROITS
  // ============================================================

  get canManageUnites(): boolean {

    return this.authService.hasRole([
      ROLES.Administrateur,
      ROLES.Gestionnaire
    ]);
  }


  /**
   * Indique si l'utilisateur dispose d'un droit
   * fonctionnel pour accéder au paiement.
   *
   * Le backend reste l'autorité finale.
   */
  get canAccessPaiement(): boolean {

    return this.authService.hasRole([
      ROLES.Locataire,
      ROLES.Administrateur,
      ROLES.Gestionnaire
    ]);
  }


  /**
   * Indique si l'utilisateur peut enregistrer
   * un paiement en espèces.
   *
   * Le backend applique également cette règle.
   */
  get canPayCash(): boolean {

    return this.authService.hasRole([
      ROLES.Administrateur,
      ROLES.Gestionnaire
    ]);
  }


  // ============================================================
  // INITIALISATION
  // ============================================================

  ngOnInit(): void {

    this.userRole =
      this.authService.currentUser()?.role || '';

    this.initForm();

    this.chargerBiens();

    this.chargerUnites();
  }


  // ============================================================
  // DESTRUCTION
  // ============================================================

  ngOnDestroy(): void {

    this.libererPrevisualisations();
  }


  // ============================================================
  // FORMULAIRE UNITÉ
  // ============================================================

  initForm(): void {

    this.uniteForm = this.fb.group({

      reference: [
        '',
        [
          Validators.required,
          Validators.maxLength(50)
        ]
      ],

      type: [
        TypeUniteLocative.Appartement,
        Validators.required
      ],

      superficie: [
        0,
        [
          Validators.required,
          Validators.min(1)
        ]
      ],

      loyer: [
        0,
        [
          Validators.required,
          Validators.min(0)
        ]
      ],

      statut: [
        StatutBien.Disponible,
        Validators.required
      ],

      bienImmobilierId: [
        null,
        Validators.required
      ]
    });
  }


  // ============================================================
  // CHARGEMENT DES BIENS
  // ============================================================

  chargerBiens(): void {

    this.chargementBiens = true;

    this.bienService
      .getBiens()
      .subscribe({

        next: (data: BienDto[]) => {

          this.biens = data ?? [];

          this.chargementBiens = false;
        },

        error: (err: unknown) => {

          console.error(
            'Erreur chargement biens:',
            err
          );

          this.chargementBiens = false;

          this.messageService.add({

            severity: 'error',

            summary: 'Erreur',

            detail:
              'Impossible de charger les biens immobiliers.'
          });
        }
      });
  }


  // ============================================================
  // CHARGEMENT DES UNITÉS
  // ============================================================

  chargerUnites(): void {

    this.chargement = true;

    this.uniteService
      .getUnites()
      .subscribe({

        next: (data: UniteLocativeDto[]) => {

          this.unites = data ?? [];

          this.chargement = false;
        },

        error: (err: unknown) => {

          console.error(
            'Erreur chargement unités:',
            err
          );

          this.chargement = false;

          this.messageService.add({

            severity: 'error',

            summary: 'Erreur',

            detail:
              'Impossible de charger la liste des unités locatives.'
          });
        }
      });
  }


  // ============================================================
  // PAIEMENT - GESTION DES MOIS
  // ============================================================

  /**
   * Convertit une date provenant de l'API en date locale
   * sans subir de décalage lié au fuseau horaire.
   *
   * Exemple :
   * "2026-10-01T00:00:00" -> 01/10/2026
   */
  private creerDateLocale(
    valeur: string | Date
  ): Date {

    if (valeur instanceof Date) {

      return new Date(
        valeur.getFullYear(),
        valeur.getMonth(),
        valeur.getDate()
      );
    }

    const texte =
      String(valeur);

    const partieDate =
      texte.substring(0, 10);

    const morceaux =
      partieDate.split('-');

    if (morceaux.length !== 3) {

      return new Date(NaN);
    }

    const annee =
      Number(morceaux[0]);

    const mois =
      Number(morceaux[1]);

    const jour =
      Number(morceaux[2]);

    return new Date(
      annee,
      mois - 1,
      jour
    );
  }


  /**
   * Retourne le premier jour du mois.
   */
  private premierJourDuMois(
    date: Date
  ): Date {

    return new Date(
      date.getFullYear(),
      date.getMonth(),
      1
    );
  }


  /**
   * Transforme une date en valeur utilisable
   * par le backend pour MoisLoyer.
   *
   * Exemple :
   * 01/10/2026 -> 2026-10-01T00:00:00
   */
  private formatMoisLoyer(
    date: Date
  ): string {

    const annee =
      date.getFullYear();

    const mois =
      String(
        date.getMonth() + 1
      ).padStart(2, '0');

    return `${annee}-${mois}-01T00:00:00`;
  }


  /**
   * Génère tous les mois couverts par le contrat.
   *
   * Exemple :
   *
   * Contrat :
   * 01/08/2026 -> 30/11/2026
   *
   * Résultat :
   * Août 2026
   * Septembre 2026
   * Octobre 2026
   * Novembre 2026
   */
  private genererMoisLoyerOptions(
    contrat: ContratUniteDto
  ): MoisLoyerOption[] {

    const dateDebut =
      this.creerDateLocale(
        contrat.dateDebut
      );

    const dateFin =
      contrat.dateFin
        ? this.creerDateLocale(
            contrat.dateFin
          )
        : new Date();

    if (
      Number.isNaN(dateDebut.getTime()) ||
      Number.isNaN(dateFin.getTime())
    ) {

      return [];
    }

    let moisCourant =
      this.premierJourDuMois(
        dateDebut
      );

    const dernierMois =
      this.premierJourDuMois(
        dateFin
      );

    const options:
      MoisLoyerOption[] = [];

    while (
      moisCourant <= dernierMois
    ) {

      const valeur =
        this.formatMoisLoyer(
          moisCourant
        );

      const libelle =
        moisCourant.toLocaleDateString(
          'fr-FR',
          {
            month: 'long',
            year: 'numeric'
          }
        );

      options.push({

        label:
          libelle.charAt(0).toUpperCase() +
          libelle.slice(1),

        value:
          valeur
      });

      moisCourant =
        new Date(
          moisCourant.getFullYear(),
          moisCourant.getMonth() + 1,
          1
        );
    }

    return options;
  }


  /**
   * Détermine le mois à présélectionner.
   *
   * Le mois courant est privilégié lorsqu'il est
   * couvert par le contrat.
   *
   * Si le contrat ne couvre pas le mois courant,
   * le premier mois du contrat est sélectionné.
   */
  private determinerMoisLoyerParDefaut(
    contrat: ContratUniteDto
  ): string {

    if (
      this.moisLoyerOptions.length === 0
    ) {

      return '';
    }

    const maintenant =
      new Date();

    const moisCourant =
      this.formatMoisLoyer(
        this.premierJourDuMois(
          maintenant
        )
      );

    const moisCourantExiste =
      this.moisLoyerOptions.some(
        option =>
          option.value === moisCourant
      );

    if (moisCourantExiste) {

      return moisCourant;
    }

    return this.moisLoyerOptions[0].value;
  }


  // ============================================================
  // OUVRIR PAIEMENT
  // ============================================================

  /**
   * Ouvre la popup de paiement.
   *
   * Le contrat actif est récupéré depuis le détail
   * de l'unité afin de disposer de son identifiant.
   */
  ouvrirPaiement(
    unite: UniteLocativeDto
  ): void {

    // ----------------------------------------------------------
    // Vérification des droits
    // ----------------------------------------------------------

    if (!this.canAccessPaiement) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Accès refusé',

        detail:
          'Vous n’êtes pas autorisé à effectuer un paiement.'
      });

      return;
    }


    // ----------------------------------------------------------
    // Vérification rapide
    // ----------------------------------------------------------

    if (!unite.aContratActif) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Paiement impossible',

        detail:
          'Cette unité ne possède aucun contrat actif.'
      });

      return;
    }


    // ----------------------------------------------------------
    // Récupération du détail de l'unité
    // ----------------------------------------------------------

    this.uniteService
      .getUniteById(unite.id)
      .subscribe({

        next: (
          data: UniteLocativeDetailDto
        ) => {

          const contrats =
            data.contrats ?? [];


          // ----------------------------------------------------
          // Recherche du contrat actif
          // ----------------------------------------------------

          const contratActif =
            contrats.find(
              contrat =>
                contrat.statut === StatutContrat.Actif
            );


          // ----------------------------------------------------
          // Aucun contrat actif
          // ----------------------------------------------------

          if (!contratActif) {

            this.messageService.add({

              severity: 'warn',

              summary: 'Paiement impossible',

              detail:
                'Aucun contrat actif n’a été trouvé pour cette unité.'
            });

            return;
          }


          // ----------------------------------------------------
          // Initialisation de la popup
          // ----------------------------------------------------

          this.unitePaiement =
            data;

          this.contratPaiement =
            contratActif;

          this.montantPaiement =
            contratActif.montantLoyer;


          // ----------------------------------------------------
          // Génération des mois de loyer
          // ----------------------------------------------------

          this.moisLoyerOptions =
            this.genererMoisLoyerOptions(
              contratActif
            );


          // ----------------------------------------------------
          // Présélection du mois
          // ----------------------------------------------------

          this.moisLoyerPaiement =
            this.determinerMoisLoyerParDefaut(
              contratActif
            );


          this.modePaiementSelectionne =
            null;

          this.telephonePaiement =
            '';

          this.displayPaiementModal =
            true;
        },

        error: (err: unknown) => {

          console.error(
            'Erreur récupération contrat actif:',
            err
          );

          this.messageService.add({

            severity: 'error',

            summary: 'Erreur',

            detail:
              'Impossible de récupérer le contrat actif de cette unité.'
          });
        }
      });
  }


  // ============================================================
  // CONFIRMATION DU PAIEMENT
  // ============================================================

  confirmerPaiement(): void {

    // ----------------------------------------------------------
    // Contrat obligatoire
    // ----------------------------------------------------------

    if (!this.contratPaiement) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Paiement impossible',

        detail:
          'Aucun contrat actif n’est associé au paiement.'
      });

      return;
    }


    // ----------------------------------------------------------
    // Mois de loyer obligatoire
    // ----------------------------------------------------------

    if (!this.moisLoyerPaiement) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Mois de loyer',

        detail:
          'Le mois de loyer à payer est obligatoire.'
      });

      return;
    }


    // ----------------------------------------------------------
    // Vérification du mois
    // ----------------------------------------------------------

    const moisSelectionneExiste =
      this.moisLoyerOptions.some(
        option =>
          option.value ===
          this.moisLoyerPaiement
      );

    if (!moisSelectionneExiste) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Mois de loyer invalide',

        detail:
          'Le mois sélectionné ne correspond pas à la période du contrat.'
      });

      return;
    }


    // ----------------------------------------------------------
    // Montant
    // ----------------------------------------------------------

    if (
      !this.montantPaiement ||
      this.montantPaiement <= 0
    ) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Montant invalide',

        detail:
          'Veuillez saisir un montant supérieur à zéro.'
      });

      return;
    }


    // ----------------------------------------------------------
    // Mode de paiement
    // ----------------------------------------------------------

    if (
      this.modePaiementSelectionne === null
    ) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Mode de paiement',

        detail:
          'Veuillez sélectionner un mode de paiement.'
      });

      return;
    }


    // ----------------------------------------------------------
    // Paiement en espèces
    // ----------------------------------------------------------

    if (
      this.modePaiementSelectionne ===
      ModePaiement.Especes
    ) {

      if (!this.canPayCash) {

        this.messageService.add({

          severity: 'warn',

          summary: 'Accès refusé',

          detail:
            'Seuls les administrateurs et gestionnaires peuvent enregistrer un paiement en espèces.'
        });

        return;
      }

      this.enregistrerPaiementEspeces();

      return;
    }


    // ----------------------------------------------------------
    // Paiement électronique
    // ----------------------------------------------------------

    if (
      !this.telephonePaiement.trim()
    ) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Téléphone obligatoire',

        detail:
          'Veuillez saisir le numéro de téléphone utilisé pour le paiement.'
      });

      return;
    }


    this.initierPaiementElectronique();
  }


  // ============================================================
  // PAIEMENT ÉLECTRONIQUE
  // ============================================================

  private initierPaiementElectronique(): void {

    if (
      !this.contratPaiement ||
      this.modePaiementSelectionne === null
    ) {

      return;
    }

    this.paiementEnCours = true;


    const request:
      InitierPaiementRequest = {

      contratId:
        this.contratPaiement.id,

      montant:
        this.montantPaiement,

      modePaiement:
        this.modePaiementSelectionne,

      telephone:
        this.telephonePaiement.trim(),

      moisLoyer:
        this.moisLoyerPaiement
    };


    console.log(
      'Paiement électronique envoyé:',
      request
    );


    this.paiementsService
      .initierPaiement(request)
      .subscribe({

        next: (
          response: PaiementResponse
        ) => {

          this.paiementEnCours = false;

          this.displayPaiementModal = false;

          this.messageService.add({

            severity: 'success',

            summary: 'Paiement initié',

            detail:
              response.message ||
              'Le paiement a été initié avec succès.'
          });

          this.reinitialiserPaiement();
        },

        error: (err: unknown) => {

          this.paiementEnCours = false;

          console.error(
            'Erreur initiation paiement:',
            err
          );

          const erreur =
            err as {
              status?: number;
              error?: {
                message?: string;
                title?: string;
              } | string;
            };


          const message =
            typeof erreur.error === 'string'
              ? erreur.error
              : erreur.error?.message ||
                erreur.error?.title ||
                'Impossible d’initier le paiement.';


          this.messageService.add({

            severity: 'error',

            summary: 'Paiement échoué',

            detail:
              message
          });
        }
      });
  }


  // ============================================================
  // PAIEMENT EN ESPÈCES
  // ============================================================

  private enregistrerPaiementEspeces(): void {

    if (!this.contratPaiement) {

      return;
    }

    this.paiementEnCours = true;


    const request:
      PaiementEspecesRequest = {

      contratId:
        this.contratPaiement.id,

      montant:
        this.montantPaiement,

      moisLoyer:
        this.moisLoyerPaiement
    };


    console.log(
      'Paiement espèces envoyé:',
      request
    );


    this.paiementsService
      .enregistrerPaiementEspeces(request)
      .subscribe({

        next: (
          response: PaiementResponse
        ) => {

          this.paiementEnCours = false;

          this.displayPaiementModal = false;

          this.messageService.add({

            severity: 'success',

            summary: 'Paiement enregistré',

            detail:
              response.message ||
              'Le paiement en espèces a été enregistré avec succès.'
          });

          this.reinitialiserPaiement();
        },

        error: (err: unknown) => {

          this.paiementEnCours = false;

          console.error(
            'Erreur paiement espèces:',
            err
          );

          const erreur =
            err as {
              status?: number;
              error?: {
                message?: string;
                title?: string;
              } | string;
            };


          const message =
            typeof erreur.error === 'string'
              ? erreur.error
              : erreur.error?.message ||
                erreur.error?.title ||
                'Impossible d’enregistrer le paiement.';


          this.messageService.add({

            severity: 'error',

            summary: 'Erreur',

            detail:
              message
          });
        }
      });
  }


  // ============================================================
  // FERMETURE POPUP PAIEMENT
  // ============================================================

  fermerPaiementModal(): void {

    if (this.paiementEnCours) {

      return;
    }

    this.displayPaiementModal = false;

    this.reinitialiserPaiement();
  }


  // ============================================================
  // RÉINITIALISATION PAIEMENT
  // ============================================================

  private reinitialiserPaiement(): void {

    this.contratPaiement = null;

    this.unitePaiement = null;

    this.montantPaiement = 0;

    this.moisLoyerPaiement = '';

    this.moisLoyerOptions = [];

    this.modePaiementSelectionne = null;

    this.telephonePaiement = '';
  }


  // ============================================================
  // AJOUT D'UNE UNITÉ
  // ============================================================

  ouvrirModalAjout(): void {

    if (!this.canManageUnites) {

      return;
    }

    this.nettoyerPhotos();

    this.isEditMode = false;

    this.selectedUniteId = null;

    this.uniteForm.reset({

      reference: '',

      type:
        TypeUniteLocative.Appartement,

      superficie: 0,

      loyer: 0,

      statut:
        StatutBien.Disponible,

      bienImmobilierId: null
    });

    this.displayModal = true;
  }


  // ============================================================
  // MODIFICATION D'UNE UNITÉ
  // ============================================================

  ouvrirModalModification(
    unite: UniteLocativeDto
  ): void {

    if (!this.canManageUnites) {

      return;
    }

    this.nettoyerPhotos();

    this.isEditMode = true;

    this.selectedUniteId =
      unite.id;

    this.uniteForm.patchValue({

      reference:
        unite.reference,

      type:
        unite.type,

      superficie:
        unite.superficie,

      loyer:
        unite.loyer,

      statut:
        unite.statut,

      bienImmobilierId:
        unite.bienImmobilierId
    });

    this.photosExistantes =
      [...(unite.photos ?? [])];

    this.displayModal = true;
  }


  // ============================================================
  // SÉLECTION DE PHOTOS
  // ============================================================

  onFichiersSelectionnes(
    event: Event
  ): void {

    const input =
      event.target as HTMLInputElement;

    if (
      !input.files ||
      input.files.length === 0
    ) {

      return;
    }

    const fichiers =
      Array.from(input.files);


    const extensionsAutorisees = [
      'image/jpeg',
      'image/png',
      'image/webp'
    ];

    const tailleMaximale =
      10 * 1024 * 1024;


    for (const fichier of fichiers) {

      if (
        !extensionsAutorisees.includes(
          fichier.type
        )
      ) {

        this.messageService.add({

          severity: 'warn',

          summary: 'Format non autorisé',

          detail:
            `Le fichier "${fichier.name}" n'est pas une image JPG, PNG ou WebP.`
        });

        continue;
      }


      if (
        fichier.size > tailleMaximale
      ) {

        this.messageService.add({

          severity: 'warn',

          summary: 'Fichier trop volumineux',

          detail:
            `Le fichier "${fichier.name}" dépasse la taille maximale de 10 Mo.`
        });

        continue;
      }


      const nombreTotal =
        this.photosExistantes.length +
        this.fichiersSelectionnes.length;

      if (nombreTotal >= 20) {

        this.messageService.add({

          severity: 'warn',

          summary: 'Limite atteinte',

          detail:
            'Une unité locative peut contenir au maximum 20 photos.'
        });

        break;
      }


      const dejaSelectionne =
        this.fichiersSelectionnes.some(
          f =>
            f.name === fichier.name &&
            f.size === fichier.size
        );

      if (dejaSelectionne) {

        continue;
      }


      this.fichiersSelectionnes.push(
        fichier
      );

      this.previsualisations.push(
        URL.createObjectURL(fichier)
      );
    }


    input.value = '';
  }


  // ============================================================
  // SUPPRIMER UNE NOUVELLE PHOTO
  // ============================================================

  supprimerNouvellePhoto(
    index: number
  ): void {

    if (
      index < 0 ||
      index >= this.fichiersSelectionnes.length
    ) {

      return;
    }

    const preview =
      this.previsualisations[index];

    if (preview) {

      URL.revokeObjectURL(
        preview
      );
    }

    this.fichiersSelectionnes.splice(
      index,
      1
    );

    this.previsualisations.splice(
      index,
      1
    );
  }


  // ============================================================
  // SUPPRIMER UNE PHOTO EXISTANTE
  // ============================================================

  supprimerPhotoExistante(
    index: number
  ): void {

    if (
      index < 0 ||
      index >= this.photosExistantes.length
    ) {

      return;
    }

    this.photosExistantes.splice(
      index,
      1
    );
  }


  // ============================================================
  // NETTOYAGE DES PHOTOS
  // ============================================================

  private nettoyerPhotos(): void {

    this.libererPrevisualisations();

    this.fichiersSelectionnes = [];

    this.photosExistantes = [];
  }


  // ============================================================
  // LIBÉRER LES URL DE PRÉVISUALISATION
  // ============================================================

  private libererPrevisualisations(): void {

    for (
      const preview of this.previsualisations
    ) {

      URL.revokeObjectURL(
        preview
      );
    }

    this.previsualisations = [];
  }


  // ============================================================
  // DÉTAIL D'UNE UNITÉ
  // ============================================================

  ouvrirDetail(
    unite: UniteLocativeDto
  ): void {

    this.uniteService
      .getUniteById(unite.id)
      .subscribe({

        next: (
          data: UniteLocativeDetailDto
        ) => {

          this.selectedUnite =
            data;

          this.displayDetailModal =
            true;
        },

        error: (err: unknown) => {

          console.error(
            'Erreur chargement détail unité:',
            err
          );

          this.messageService.add({

            severity: 'error',

            summary: 'Erreur',

            detail:
              'Impossible de charger le détail de cette unité.'
          });
        }
      });
  }


  // ============================================================
  // SAUVEGARDE
  // ============================================================

  sauvegarder(): void {

    if (!this.canManageUnites) {

      return;
    }

    if (this.uniteForm.invalid) {

      this.uniteForm.markAllAsTouched();

      return;
    }

    const valeurs =
      this.uniteForm.getRawValue();


    const reference =
      String(valeurs.reference)
        .trim();

    const type: TypeUniteLocative =
      Number(valeurs.type);

    const superficie =
      Number(valeurs.superficie);

    const loyer =
      Number(valeurs.loyer);

    const statut: StatutBien =
      Number(valeurs.statut);

    const bienImmobilierId =
      String(valeurs.bienImmobilierId);


    if (!reference) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Validation',

        detail:
          'La référence de l’unité est obligatoire.'
      });

      return;
    }


    if (superficie <= 0) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Validation',

        detail:
          'La superficie doit être supérieure à zéro.'
      });

      return;
    }


    if (loyer < 0) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Validation',

        detail:
          'Le loyer ne peut pas être négatif.'
      });

      return;
    }


    if (!bienImmobilierId) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Validation',

        detail:
          'Veuillez sélectionner un bien immobilier.'
      });

      return;
    }


    // ==========================================================
    // CRÉATION
    // ==========================================================

    if (!this.isEditMode) {

      const creation:
        CreerUniteLocativeRequest = {

        reference,

        type,

        superficie,

        loyer,

        statut,

        bienImmobilierId,

        fichiers:
          [...this.fichiersSelectionnes]
      };


      this.uniteService
        .createUnite(creation)
        .subscribe({

          next: () => {

            this.messageService.add({

              severity: 'success',

              summary: 'Succès',

              detail:
                'Unité locative créée avec succès.'
            });

            this.displayModal =
              false;

            this.nettoyerPhotos();

            this.chargerUnites();
          },

          error: (err: unknown) => {

            this.gererErreurSauvegarde(
              err,
              'Erreur lors de la création de l’unité.'
            );
          }
        });

      return;
    }


    // ==========================================================
    // MODIFICATION
    // ==========================================================

    if (
      this.isEditMode &&
      this.selectedUniteId
    ) {

      const modification:
        ModifierUniteLocativeRequest = {

        reference,

        type,

        superficie,

        loyer,

        statut,

        bienImmobilierId,

        photosExistantes:
          [...this.photosExistantes],

        fichiers:
          [...this.fichiersSelectionnes]
      };


      this.uniteService
        .updateUnite(
          this.selectedUniteId,
          modification
        )
        .subscribe({

          next: () => {

            this.messageService.add({

              severity: 'success',

              summary: 'Succès',

              detail:
                'Unité locative modifiée avec succès.'
            });

            this.displayModal =
              false;

            this.nettoyerPhotos();

            this.chargerUnites();
          },

          error: (err: unknown) => {

            this.gererErreurSauvegarde(
              err,
              'Erreur lors de la modification de l’unité.'
            );
          }
        });
    }
  }


  // ============================================================
  // FERMETURE DE LA MODALE UNITÉ
  // ============================================================

  fermerModal(): void {

    this.displayModal = false;

    this.nettoyerPhotos();

    this.selectedUniteId = null;
  }


  // ============================================================
  // GESTION DES ERREURS
  // ============================================================

  private gererErreurSauvegarde(
    err: unknown,
    messageDefaut: string
  ): void {

    console.error(
      'Erreur sauvegarde unité:',
      err
    );

    const erreur =
      err as {
        status?: number;
        error?: {
          message?: string;
          title?: string;
        } | string;
      };


    const errorMessage =
      typeof erreur.error === 'string'
        ? erreur.error
        : erreur.error?.message ||
          erreur.error?.title ||
          '';

    const messageNormalise =
      errorMessage.toLowerCase();


    if (
      erreur.status === 409 ||
      messageNormalise.includes('référence') ||
      messageNormalise.includes('reference') ||
      messageNormalise.includes('existe déjà')
    ) {

      this.messageService.add({

        severity: 'error',

        summary:
          'Référence existante',

        detail:
          'Une unité possède déjà cette référence dans ce bien.'
      });

      return;
    }


    this.messageService.add({

      severity: 'error',

      summary: 'Erreur',

      detail:
        errorMessage ||
        messageDefaut
    });
  }


  // ============================================================
  // SUPPRESSION / ARCHIVAGE
  // ============================================================

  supprimerUnite(
    unite: UniteLocativeDto
  ): void {

    if (!this.canManageUnites) {

      return;
    }

    this.confirmationService.confirm({

      message:
        `Voulez-vous vraiment archiver l’unité ${unite.reference} ?`,

      header:
        'Confirmation de suppression',

      icon:
        'pi pi-exclamation-triangle',

      acceptLabel:
        'Oui',

      rejectLabel:
        'Non',

      accept: () => {

        this.uniteService
          .deleteUnite(unite.id)
          .subscribe({

            next: () => {

              this.messageService.add({

                severity: 'success',

                summary: 'Succès',

                detail:
                  'Unité locative archivée avec succès.'
              });

              this.chargerUnites();
            },

            error: (err: unknown) => {

              console.error(
                'Erreur suppression unité:',
                err
              );

              const erreur =
                err as {
                  status?: number;
                  error?: {
                    message?: string;
                    title?: string;
                  } | string;
                };


              const message =
                typeof erreur.error === 'string'
                  ? erreur.error
                  : erreur.error?.message ||
                    erreur.error?.title ||
                    'Erreur lors de la suppression.';


              this.messageService.add({

                severity:
                  erreur.status === 409
                    ? 'warn'
                    : 'error',

                summary:
                  erreur.status === 409
                    ? 'Suppression impossible'
                    : 'Erreur',

                detail:
                  message
              });
            }
          });
      }
    });
  }


  // ============================================================
  // MODIFICATION RAPIDE DU STATUT
  // ============================================================

  modifierStatut(
    unite: UniteLocativeDto,
    statut: StatutBien
  ): void {

    if (!this.canManageUnites) {

      return;
    }

    if (unite.statut === statut) {

      return;
    }

    this.uniteService
      .updateStatut(
        unite.id,
        statut
      )
      .subscribe({

        next: (response) => {

          unite.statut =
            response.statut;

          this.messageService.add({

            severity: 'success',

            summary: 'Succès',

            detail:
              response.message ||
              'Statut modifié avec succès.'
          });
        },

        error: (err: unknown) => {

          console.error(
            'Erreur modification statut:',
            err
          );

          const erreur =
            err as {
              error?: {
                message?: string;
                title?: string;
              } | string;
            };


          const message =
            typeof erreur.error === 'string'
              ? erreur.error
              : erreur.error?.message ||
                erreur.error?.title ||
                'Impossible de modifier le statut.';

          this.messageService.add({

            severity: 'error',

            summary: 'Erreur',

            detail: message
          });
        }
      });
  }


  // ============================================================
  // LIBELLÉ TYPE
  // ============================================================

  getTypeLibelle(
    type: TypeUniteLocative
  ): string {

    return this.uniteService
      .getTypeLibelle(type);
  }


  // ============================================================
  // LIBELLÉ STATUT
  // ============================================================

  getStatutLibelle(
    statut: StatutBien
  ): string {

    return this.uniteService
      .getStatutLibelle(statut);
  }


  // ============================================================
  // SEVERITY STATUT
  // ============================================================

  getStatutSeverity(
    statut: StatutBien
  ): 'success' | 'info' | 'warn' | 'danger' {

    return this.uniteService
      .getStatutSeverity(statut);
  }


  // ============================================================
  // URL PHOTO
  // ============================================================

  getPhotoUrl(
    photo: string
  ): string {

    return this.uniteService
      .getPhotoUrl(photo);
  }


  // ============================================================
  // BIEN : LIBELLÉ POUR LE SELECT
  // ============================================================

  getBienLibelle(
    bien: BienDto
  ): string {

    return `${bien.reference} - ${bien.ville}`;
  }


  // ============================================================
  // BIEN SÉLECTIONNÉ
  // ============================================================

  getBienSelectionne():
    BienDto | undefined {

    const bienId =
      this.uniteForm?.get(
        'bienImmobilierId'
      )?.value;

    return this.biens.find(
      b => b.id === bienId
    );
  }
}
