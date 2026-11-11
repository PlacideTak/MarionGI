
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
  TypePaiement,
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
export class UnitesLocatives implements OnInit, OnDestroy {

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

  fichiersSelectionnes: File[] = [];

  previsualisations: string[] = [];

  photosExistantes: string[] = [];


  // ============================================================
  // DÉTAIL
  // ============================================================

  selectedUnite: UniteLocativeDetailDto | null = null;

  displayDetailModal = false;


  // ============================================================
  // PAIEMENT
  // ============================================================

  displayPaiementModal = false;

  paiementEnCours = false;

  unitePaiement: UniteLocativeDetailDto | null = null;

  contratPaiement: ContratUniteDto | null = null;

  /**
   * Type de paiement sélectionné.
   *
   * Par défaut, le paiement concerne le loyer.
   */
  typePaiementSelectionne: TypePaiement =
    TypePaiement.Loyer;

  /**
   * Montant du paiement.
   *
   * Le montant est initialisé selon le type de paiement,
   * mais reste modifiable pour permettre les paiements partiels.
   */
  montantPaiement = 0;

  /**
   * Mois concerné par le paiement.
   *
   * Obligatoire pour le loyer et les charges locatives.
   * Facultatif pour la caution et les autres paiements.
   */
  moisLoyerPaiement = '';

  moisLoyerOptions: MoisLoyerOption[] = [];

  modePaiementSelectionne: ModePaiement | null = null;

  telephonePaiement = '';

  readonly ModePaiement = ModePaiement;

  readonly TypePaiement = TypePaiement;

  /**
   * Types de paiement proposés dans la popup.
   */
  readonly typesPaiement = [
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

  /**
   * Indique si le mois de paiement doit être renseigné.
   */
  get moisLoyerRequis(): boolean {
    return this.typePaiementSelectionne === TypePaiement.Loyer ||
          this.typePaiementSelectionne === TypePaiement.ChargesLocatives;
  }

  /**
   * Indique si la sélection du mois doit être affichée.
   *
   * Pour la caution et les autres paiements, le mois
   * n'est pas nécessaire.
   */
  get afficherSelectionMois(): boolean {

    return this.moisLoyerRequis;
  }

  /**
   * Modes de paiement disponibles.
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

  get canAccessPaiement(): boolean {

    return this.authService.hasRole([
      ROLES.Locataire,
      ROLES.Administrateur,
      ROLES.Gestionnaire
    ]);
  }

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

      nombrePieces: [null],

      etage: [null],

      loyer: [
        0,
        [
          Validators.required,
          Validators.min(0)
        ]
      ],

      chargesLocativesMensuelles: [
        null,
        Validators.min(0)
      ],

      montantCaution: [
        null,
        Validators.min(0)
      ],

      statut: [
        StatutBien.Disponible,
        Validators.required
      ],

      datePremiereMiseEnLocation: [null],

      dateDerniereRenovation: [null],

      estExonereeFiscalement: [false],

      motifExoneration: [null],

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

    const texte = String(valeur);

    const partieDate = texte.substring(0, 10);

    const morceaux = partieDate.split('-');

    if (morceaux.length !== 3) {

      return new Date(NaN);
    }

    const annee = Number(morceaux[0]);

    const mois = Number(morceaux[1]);

    const jour = Number(morceaux[2]);

    return new Date(
      annee,
      mois - 1,
      jour
    );
  }

  private premierJourDuMois(
    date: Date
  ): Date {

    return new Date(
      date.getFullYear(),
      date.getMonth(),
      1
    );
  }

  private formatMoisLoyer(
    date: Date
  ): string {

    const annee = date.getFullYear();

    const mois = String(
      date.getMonth() + 1
    ).padStart(2, '0');

    return `${annee}-${mois}-01T00:00:00`;
  }

  private genererMoisLoyerOptions(
    contrat: ContratUniteDto
  ): MoisLoyerOption[] {

    const dateDebut = this.creerDateLocale(
      contrat.dateDebut
    );

    const dateFin = contrat.dateFin
      ? this.creerDateLocale(contrat.dateFin)
      : new Date();

    if (
      Number.isNaN(dateDebut.getTime()) ||
      Number.isNaN(dateFin.getTime())
    ) {

      return [];
    }

    let moisCourant = this.premierJourDuMois(
      dateDebut
    );

    const dernierMois = this.premierJourDuMois(
      dateFin
    );

    const options: MoisLoyerOption[] = [];

    while (moisCourant <= dernierMois) {

      const valeur = this.formatMoisLoyer(
        moisCourant
      );

      const libelle = moisCourant.toLocaleDateString(
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
        value: valeur
      });

      moisCourant = new Date(
        moisCourant.getFullYear(),
        moisCourant.getMonth() + 1,
        1
      );
    }

    return options;
  }

  private determinerMoisLoyerParDefaut(
    contrat: ContratUniteDto
  ): string {

    if (this.moisLoyerOptions.length === 0) {

      return '';
    }

    const maintenant = new Date();

    const moisCourant = this.formatMoisLoyer(
      this.premierJourDuMois(maintenant)
    );

    const moisCourantExiste =
      this.moisLoyerOptions.some(
        option => option.value === moisCourant
      );

    if (moisCourantExiste) {

      return moisCourant;
    }

    return this.moisLoyerOptions[0].value;
  }


  // ============================================================
  // PAIEMENT - CHANGEMENT DU TYPE
  // ============================================================

  /**
   * Met à jour le montant proposé et le mois
   * en fonction du type de paiement sélectionné.
   *
   * Le montant reste modifiable par l'utilisateur.
   */

onTypePaiementChange(type: TypePaiement | string | number): void {

  const typeNumerique = Number(type);

  this.typePaiementSelectionne = typeNumerique as TypePaiement;


  if (!this.contratPaiement) {
    return;
  }

  switch (this.typePaiementSelectionne) {

    case TypePaiement.Loyer:
      this.montantPaiement = this.contratPaiement.montantLoyer ?? 0;
      this.moisLoyerPaiement =
        this.determinerMoisLoyerParDefaut(this.contratPaiement);
      break;

    case TypePaiement.ChargesLocatives:
      this.montantPaiement =
        this.contratPaiement.montantChargesLocatives ?? 0;
      this.moisLoyerPaiement =
        this.determinerMoisLoyerParDefaut(this.contratPaiement);
      break;

    case TypePaiement.Caution:

      this.montantPaiement =
        this.contratPaiement.montantCaution ?? 0;

      this.moisLoyerPaiement = '';

      break;

    case TypePaiement.Autre:

      this.montantPaiement = 0;
      this.moisLoyerPaiement = '';

      break;

    default:

      this.montantPaiement = 0;
      this.moisLoyerPaiement = '';

      break;
  }
}


  // ============================================================
  // OUVRIR PAIEMENT
  // ============================================================

  ouvrirPaiement(
    unite: UniteLocativeDto
  ): void {

    if (!this.canAccessPaiement) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Accès refusé',
        detail:
          'Vous n’êtes pas autorisé à effectuer un paiement.'
      });

      return;
    }

    if (!unite.aContratActif) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Paiement impossible',
        detail:
          'Cette unité ne possède aucun contrat actif.'
      });

      return;
    }

    this.uniteService
      .getUniteById(unite.id)
      .subscribe({

        next: (data: UniteLocativeDetailDto) => {

          const contrats = data.contrats ?? [];

          const contratActif = contrats.find(
            contrat =>
              contrat.statut === StatutContrat.Actif
          );

          if (!contratActif) {

            this.messageService.add({
              severity: 'warn',
              summary: 'Paiement impossible',
              detail:
                'Aucun contrat actif n’a été trouvé pour cette unité.'
            });

            return;
          }

          this.unitePaiement = data;

          this.contratPaiement = contratActif;

          this.moisLoyerOptions =
            this.genererMoisLoyerOptions(
              contratActif
            );

          this.typePaiementSelectionne =
            TypePaiement.Loyer;

          this.montantPaiement =
            contratActif.montantLoyer ?? 0;

          this.moisLoyerPaiement =
            this.determinerMoisLoyerParDefaut(
              contratActif
            );

          this.modePaiementSelectionne = null;

          this.telephonePaiement = '';

          this.displayPaiementModal = true;
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
    // Vérification du type de paiement
    // ----------------------------------------------------------

    if (
      ![
        TypePaiement.Loyer,
        TypePaiement.ChargesLocatives,
        TypePaiement.Caution,
        TypePaiement.Autre
      ].includes(this.typePaiementSelectionne)
    ) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Type de paiement',
        detail:
          'Veuillez sélectionner un type de paiement valide.'
      });

      return;
    }

    // ----------------------------------------------------------
    // Vérification du mois
    // ----------------------------------------------------------

    if (
      this.moisLoyerRequis &&
      !this.moisLoyerPaiement
    ) {

      this.messageService.add({
        severity: 'warn',
        summary: 'Mois de paiement',
        detail:
          'Veuillez sélectionner le mois concerné par ce paiement.'
      });

      return;
    }

    // ----------------------------------------------------------
    // Vérification de la période du contrat
    // ----------------------------------------------------------

    if (
      this.moisLoyerRequis &&
      this.moisLoyerPaiement
    ) {

      const moisSelectionneExiste =
        this.moisLoyerOptions.some(
          option =>
            option.value === this.moisLoyerPaiement
        );

      if (!moisSelectionneExiste) {

        this.messageService.add({
          severity: 'warn',
          summary: 'Mois invalide',
          detail:
            'Le mois sélectionné ne correspond pas à la période du contrat.'
        });

        return;
      }
    }

    // ----------------------------------------------------------
    // Montant
    // ----------------------------------------------------------

    if (
      this.montantPaiement === null ||
      this.montantPaiement === undefined ||
      !Number.isFinite(Number(this.montantPaiement)) ||
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

    if (this.modePaiementSelectionne === null) {

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
      this.modePaiementSelectionne === ModePaiement.Especes
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

    if (!this.telephonePaiement.trim()) {

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

    const request: InitierPaiementRequest = {

      contratId:
        this.contratPaiement.id,

      montant:
        this.montantPaiement,

      typePaiement:
        this.typePaiementSelectionne,

      modePaiement:
        this.modePaiementSelectionne,

      telephone:
        this.telephonePaiement.trim(),

      moisLoyer:
        this.moisLoyerRequis
          ? this.moisLoyerPaiement
          : null
    };

    console.log(
      'Paiement électronique envoyé:',
      request
    );

    this.paiementsService
      .initierPaiement(request)
      .subscribe({

        next: (response: PaiementResponse) => {

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

          const erreur = err as {
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
            detail: message
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

    const request: PaiementEspecesRequest = {

      contratId:
        this.contratPaiement.id,

      montant:
        this.montantPaiement,

      typePaiement:
        this.typePaiementSelectionne,

      moisLoyer:
        this.moisLoyerRequis
          ? this.moisLoyerPaiement
          : null
    };

    console.log(
      'Paiement espèces envoyé:',
      request
    );

    this.paiementsService
      .enregistrerPaiementEspeces(request)
      .subscribe({

        next: (response: PaiementResponse) => {

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

          const erreur = err as {
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
            detail: message
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

    this.typePaiementSelectionne =
      TypePaiement.Loyer;

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
      type: TypeUniteLocative.Appartement,
      superficie: 0,
      nombrePieces: null,
      etage: null,
      loyer: 0,
      chargesLocativesMensuelles: null,
      montantCaution: null,
      statut: StatutBien.Disponible,
      datePremiereMiseEnLocation: null,
      dateDerniereRenovation: null,
      estExonereeFiscalement: false,
      motifExoneration: null,
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

    this.selectedUniteId = unite.id;

    this.uniteForm.reset({
      reference: unite.reference ?? '',
      type: unite.type,
      superficie: unite.superficie ?? 0,

      nombrePieces: unite.nombrePieces ?? null,

      etage: unite.etage ?? null,

      loyer: unite.loyer ?? 0,

      chargesLocativesMensuelles:
        unite.chargesLocativesMensuelles ?? null,

      montantCaution:
        unite.montantCaution ?? null,

      statut: unite.statut,

      datePremiereMiseEnLocation:
        unite.datePremiereMiseEnLocation
          ? String(unite.datePremiereMiseEnLocation)
              .substring(0, 10)
          : null,

      dateDerniereRenovation:
        unite.dateDerniereRenovation
          ? String(unite.dateDerniereRenovation)
              .substring(0, 10)
          : null,

      estExonereeFiscalement:
        unite.estExonereeFiscalement ?? false,

      motifExoneration:
        unite.motifExoneration ?? null,

      bienImmobilierId:
        unite.bienImmobilierId
    });

    this.photosExistantes = [
      ...(unite.photos ?? [])
    ];

    this.displayModal = true;
  }


  // ============================================================
  // SÉLECTION DE PHOTOS
  // ============================================================

  onFichiersSelectionnes(
    event: Event
  ): void {

    const input = event.target as HTMLInputElement;

    if (!input.files || input.files.length === 0) {

      return;
    }

    const fichiers = Array.from(input.files);

    const extensionsAutorisees = [
      'image/jpeg',
      'image/png',
      'image/webp'
    ];

    const tailleMaximale = 10 * 1024 * 1024;

    for (const fichier of fichiers) {

      if (!extensionsAutorisees.includes(fichier.type)) {

        this.messageService.add({
          severity: 'warn',
          summary: 'Format non autorisé',
          detail:
            `Le fichier "${fichier.name}" n'est pas une image JPG, PNG ou WebP.`
        });

        continue;
      }

      if (fichier.size > tailleMaximale) {

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

      this.fichiersSelectionnes.push(fichier);

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

    const preview = this.previsualisations[index];

    if (preview) {

      URL.revokeObjectURL(preview);
    }

    this.fichiersSelectionnes.splice(index, 1);

    this.previsualisations.splice(index, 1);
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

    this.photosExistantes.splice(index, 1);
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

    for (const preview of this.previsualisations) {

      URL.revokeObjectURL(preview);
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

        next: (data: UniteLocativeDetailDto) => {

          this.selectedUnite = data;

          this.displayDetailModal = true;
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

    if (this.uniteForm.invalid) {

      this.uniteForm.markAllAsTouched();

      return;
    }

    const valeurs = this.uniteForm.getRawValue();

    const donneesCommunes = {

      reference: valeurs.reference,

      type: valeurs.type,

      superficie: Number(valeurs.superficie),

      nombrePieces:
        valeurs.nombrePieces !== null &&
        valeurs.nombrePieces !== ''
          ? Number(valeurs.nombrePieces)
          : null,

      etage:
        valeurs.etage !== null &&
        valeurs.etage !== ''
          ? Number(valeurs.etage)
          : null,

      loyer: Number(valeurs.loyer),

      chargesLocativesMensuelles:
        valeurs.chargesLocativesMensuelles !== null &&
        valeurs.chargesLocativesMensuelles !== ''
          ? Number(valeurs.chargesLocativesMensuelles)
          : null,

      montantCaution:
        valeurs.montantCaution !== null &&
        valeurs.montantCaution !== ''
          ? Number(valeurs.montantCaution)
          : null,

      statut: valeurs.statut,

      datePremiereMiseEnLocation:
        valeurs.datePremiereMiseEnLocation || null,

      dateDerniereRenovation:
        valeurs.dateDerniereRenovation || null,

      estExonereeFiscalement:
        valeurs.estExonereeFiscalement,

      motifExoneration:
        valeurs.motifExoneration || null,

      bienImmobilierId:
        valeurs.bienImmobilierId
    };

    if (!this.isEditMode) {

      const creation: CreerUniteLocativeRequest = {
        ...donneesCommunes,
        fichiers: [...this.fichiersSelectionnes]
      };

      this.uniteService
        .createUnite(creation)
        .subscribe({

          next: () => {

            this.messageService.add({
              severity: 'success',
              summary: 'Succès',
              detail: 'Unité locative créée.'
            });

            this.chargerUnites();

            this.fermerModal();
          },

          error: (erreur) => {

            console.error(
              'Erreur lors de la création :',
              erreur
            );

            this.messageService.add({
              severity: 'error',
              summary: 'Erreur',
              detail:
                'Impossible de créer l’unité locative.'
            });
          }
        });

      return;
    }

    if (!this.selectedUniteId) {

      return;
    }

    const modification: ModifierUniteLocativeRequest = {
      ...donneesCommunes,
      photosExistantes: [...this.photosExistantes],
      fichiers: [...this.fichiersSelectionnes]
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
            detail: 'Unité locative modifiée.'
          });

          this.chargerUnites();

          this.fermerModal();
        },

        error: (erreur) => {

          console.error(
            'Erreur lors de la modification :',
            erreur
          );

          this.messageService.add({
            severity: 'error',
            summary: 'Erreur',
            detail:
              'Impossible de modifier l’unité locative.'
          });
        }
      });
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

    const erreur = err as {
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
        summary: 'Référence existante',
        detail:
          'Une unité possède déjà cette référence dans ce bien.'
      });

      return;
    }

    this.messageService.add({
      severity: 'error',
      summary: 'Erreur',
      detail:
        errorMessage || messageDefaut
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

              const erreur = err as {
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

                detail: message
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

          unite.statut = response.statut;

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

          const erreur = err as {
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

    return this.uniteService.getTypeLibelle(type);
  }


  // ============================================================
  // LIBELLÉ STATUT
  // ============================================================

  getStatutLibelle(
    statut: StatutBien
  ): string {

    return this.uniteService.getStatutLibelle(statut);
  }


  // ============================================================
  // SEVERITY STATUT
  // ============================================================

  getStatutSeverity(
    statut: StatutBien
  ): 'success' | 'info' | 'warn' | 'danger' {

    return this.uniteService.getStatutSeverity(statut);
  }


  // ============================================================
  // URL PHOTO
  // ============================================================

  getPhotoUrl(
    photo: string
  ): string {

    return this.uniteService.getPhotoUrl(photo);
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

  getBienSelectionne(): BienDto | undefined {

    const bienId =
      this.uniteForm?.get('bienImmobilierId')?.value;

    return this.biens.find(
      b => b.id === bienId
    );
  }


  // ============================================================
  // INDICATEURS DU TABLEAU DE BORD
  // ============================================================

  get nombreUnitesTotal(): number {

    return this.unites.length;
  }

  get nombreUnitesDisponibles(): number {

    return this.unites.filter(
      unite => unite.statut === StatutBien.Disponible
    ).length;
  }

  get nombreUnitesOccupees(): number {

    return this.unites.filter(
      unite => unite.statut === StatutBien.Loue
    ).length;
  }

  get loyerMensuelTotal(): number {

    return this.unites.reduce(
      (total, unite) =>
        total + (unite.loyer ?? 0),
      0
    );
  }


  // ============================================================
  // FILTRES
  // ============================================================

  rechercheUnite = '';

  typeFiltre: TypeUniteLocative | null = null;

  statutFiltre: StatutBien | null = null;

  appliquerRecherche(table: any): void {

    table.filterGlobal(
      this.rechercheUnite.trim(),
      'contains'
    );
  }

  appliquerFiltreType(table: any): void {

    if (this.typeFiltre === null) {

      table.filter(null, 'type', 'equals');

      return;
    }

    table.filter(
      this.typeFiltre,
      'type',
      'equals'
    );
  }

  appliquerFiltreStatut(table: any): void {

    if (this.statutFiltre === null) {

      table.filter(null, 'statut', 'equals');

      return;
    }

    table.filter(
      this.statutFiltre,
      'statut',
      'equals'
    );
  }

  reinitialiserFiltres(table: any): void {

    this.rechercheUnite = '';

    this.typeFiltre = null;

    this.statutFiltre = null;

    table.clear();
  }

}
