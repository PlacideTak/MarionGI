import { Component, OnInit, inject } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
  FormsModule
} from '@angular/forms';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';

// PrimeNG
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import {
  MessageService,
  ConfirmationService
} from 'primeng/api';

// Services
import { ContratsService } from '../services/contrats.service';
import { UnitesLocativesService } from '../services/uniteslocatives.service';
import { UtilisateursService } from '../services/utilisateurs.service';
import { BiensService } from '../services/biens.service';

// Models
import {
  ContratDto,
  CreerContratRequest,
  ModifierContratRequest,
  UniteLocativeDto,
  ROLES,
  StatutContrat
} from '../models/gestimmo.models';

import { AuthService } from '../login/auth.service';


interface DropdownItem {
  label: string;
  value: string;
}


@Component({
  selector: 'app-contrats',
  standalone: true,

  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,

    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    TagModule,
    ToastModule,
    ConfirmDialogModule
  ],

  providers: [
    MessageService,
    ConfirmationService
  ],

  templateUrl: './contrats.html',
  styleUrl: './contrats.scss'
})
export class Contrats implements OnInit {

  // ============================================================
  // DEPENDANCES
  // ============================================================

  private readonly fb = inject(FormBuilder);

  private readonly contratsService =
    inject(ContratsService);

  private readonly biensService =
    inject(BiensService);

  private readonly unitesLocativesService =
    inject(UnitesLocativesService);

  private readonly utilisateursService =
    inject(UtilisateursService);

  private readonly messageService =
    inject(MessageService);

  private readonly confirmationService =
    inject(ConfirmationService);

  private readonly authService =
    inject(AuthService);


  // ============================================================
  // DONNÉES
  // ============================================================

  contrats: ContratDto[] = [];

  /**
   * Biens utilisés uniquement pour filtrer
   * les unités locatives.
   *
   * bienImmobilierId n'est jamais envoyé
   * à l'API pour un contrat.
   */
  biensOptions: DropdownItem[] = [];

  /**
   * Unités appartenant au bien sélectionné.
   */
  unitesLocativesOptions: DropdownItem[] = [];

  /**
   * Locataires disponibles.
   */
  locatairesOptions: DropdownItem[] = [];


  // ============================================================
  // DROITS
  // ============================================================

  canManageContrat = false;


  // ============================================================
  // DIALOGUE / FORMULAIRE
  // ============================================================

  /**
   * Contrôle l'ouverture du p-dialog.
   *
   * IMPORTANT :
   * Le HTML doit utiliser :
   *
   * [(visible)]="contratDialog"
   */
  contratDialog = false;

  contratForm!: FormGroup;

  isEditMode = false;

  selectedContratId?: string;


  // ============================================================
  // STATUTS
  // ============================================================

  statutOptions = [
    {
      label: 'Actif',
      value: StatutContrat.Actif
    },
    {
      label: 'En Attente',
      value: StatutContrat.EnAttente
    },
    {
      label: 'Expiré',
      value: StatutContrat.Expire
    },
    {
      label: 'Résilié',
      value: StatutContrat.Resilie
    }
  ];


  // ============================================================
  // INITIALISATION
  // ============================================================

  ngOnInit(): void {

    this.verifierPermissions();

    this.initForm();

    this.chargerContrats();

    if (this.canManageContrat) {

      this.chargerBiens();

      this.chargerLocataires();
    }
  }


  // ============================================================
  // FORMULAIRE
  // ============================================================

  private initForm(): void {

    this.contratForm = this.fb.group({

      /**
       * Référence du contrat.
       *
       * Obligatoire.
       * Maximum 50 caractères.
       *
       * Création : envoyée à l'API.
       * Modification : affichée mais non envoyée.
       */
      reference: [
        '',
        [
          Validators.required,
          Validators.maxLength(50)
        ]
      ],

      /**
       * Champ Angular uniquement.
       *
       * Sert à déterminer les unités locatives
       * appartenant au bien sélectionné.
       *
       * PAS envoyé à l'API.
       */
      bienImmobilierId: [
        '',
        Validators.required
      ],

      /**
       * FK réelle du contrat.
       */
      uniteLocativeId: [
        '',
        Validators.required
      ],

      locataireId: [
        '',
        Validators.required
      ],

      dateDebut: [
        null,
        Validators.required
      ],

      dateFin: [
        null,
        Validators.required
      ],

      montantLoyer: [
        null,
        [
          Validators.required,
          Validators.min(0)
        ]
      ],

      montantCaution: [
        0,
        [
          Validators.required,
          Validators.min(0)
        ]
      ],

      frequencePaiement: [
        1,
        [
          Validators.required,
          Validators.min(1)
        ]
      ],

      delaiJoursTolerance: [
        5,
        [
          Validators.required,
          Validators.min(0)
        ]
      ],

      /**
       * Utilisé uniquement lors de la modification.
       *
       * Lors de la création, le backend impose
       * le statut Actif.
       */
      statut: [
        StatutContrat.Actif,
        Validators.required
      ]
    });
  }


  // ============================================================
  // CHARGEMENT DES CONTRATS
  // ============================================================

  chargerContrats(): void {

    this.contratsService
      .getContrats()
      .subscribe({

        next: (data: ContratDto[]) => {

          this.contrats =
            Array.isArray(data)
              ? [...data]
              : [];
        },

        error: (err: HttpErrorResponse) => {

          this.traiterErreurHttp(
            err,
            'chargement'
          );
        }
      });
  }


  // ============================================================
  // CHARGEMENT DES BIENS
  // ============================================================

  private chargerBiens(): void {

    this.biensService
      .getBiens()
      .subscribe({

        next: (biens) => {

          if (!Array.isArray(biens)) {

            this.biensOptions = [];

            return;
          }

          this.biensOptions =
            biens.map(bien => ({

              label:
                `${bien.reference} - ${bien.adresse}`,

              value:
                bien.id
            }));
        },

        error: (err: HttpErrorResponse) => {

          console.error(
            'Erreur chargement biens immobiliers :',
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


  // ============================================================
  // CHANGEMENT DE BIEN
  // ============================================================

  onBienChange(
    bienId: string | null | undefined
  ): void {

    this.unitesLocativesOptions = [];

    this.contratForm.patchValue({
      uniteLocativeId: ''
    });

    if (!bienId) {
      return;
    }

    this.unitesLocativesService
      .getUnitesParBien(bienId)
      .subscribe({

        next: (unites: UniteLocativeDto[]) => {

          if (!Array.isArray(unites)) {

            this.unitesLocativesOptions = [];

            return;
          }

          this.unitesLocativesOptions =
            unites.map(
              (unite: UniteLocativeDto) => ({

                label:
                  unite.reference,

                value:
                  unite.id
              })
            );
        },

        error: (err: HttpErrorResponse) => {

          console.error(
            'Erreur chargement unités du bien :',
            err
          );

          this.messageService.add({

            severity: 'error',

            summary: 'Erreur',

            detail:
              'Impossible de charger les unités locatives de ce bien.'
          });
        }
      });
  }


  // ============================================================
  // LOCATAIRES
  // ============================================================

  private chargerLocataires(): void {

    this.utilisateursService
      .getLocataires()
      .subscribe({

        next: (locataires) => {

          if (!Array.isArray(locataires)) {

            this.locatairesOptions = [];

            return;
          }

          this.locatairesOptions =
            locataires.map(locataire => ({

              label:
                `${locataire.prenom ?? ''} ${locataire.nom ?? ''}`
                  .trim()
                || ROLES.Locataire,

              value:
                locataire.id
            }));
        },

        error: (err: HttpErrorResponse) => {

          console.error(
            'Erreur chargement locataires :',
            err
          );

          this.messageService.add({

            severity: 'error',

            summary: 'Erreur',

            detail:
              'Impossible de charger les locataires.'
          });
        }
      });
  }


  // ============================================================
  // UTILITAIRES
  // ============================================================

  getShortId(id?: string): string {

    return id
      ? id.substring(0, 8).toUpperCase()
      : 'N/A';
  }


  getStatutLabel(
    statut: StatutContrat | number
  ): string {

    switch (Number(statut)) {

      case StatutContrat.Actif:
        return 'Actif';

      case StatutContrat.EnAttente:
        return 'En Attente';

      case StatutContrat.Expire:
        return 'Expiré';

      case StatutContrat.Resilie:
        return 'Résilié';

      default:
        return 'Inconnu';
    }
  }


  getSeverity(
    statut: StatutContrat | number | string
  ):
    | 'success'
    | 'info'
    | 'warn'
    | 'danger'
    | undefined {

    if (typeof statut === 'string') {

      switch (statut.toLowerCase()) {

        case 'actif':
          return 'success';

        case 'enattente':
        case 'en attente':
          return 'info';

        case 'expire':
        case 'expiré':
          return 'warn';

        case 'resilie':
        case 'résilié':
          return 'danger';

        default:
          return undefined;
      }
    }

    switch (Number(statut)) {

      case StatutContrat.Actif:
        return 'success';

      case StatutContrat.EnAttente:
        return 'info';

      case StatutContrat.Expire:
        return 'warn';

      case StatutContrat.Resilie:
        return 'danger';

      default:
        return undefined;
    }
  }


  // ============================================================
  // CRÉATION
  // ============================================================

  openNew(): void {

    if (!this.canManageContrat) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Accès refusé',

        detail:
          'Vous n\'avez pas les droits nécessaires pour créer un contrat.'
      });

      return;
    }

    this.isEditMode = false;

    this.selectedContratId = undefined;

    this.unitesLocativesOptions = [];

    this.contratForm.reset({

      reference: '',

      bienImmobilierId: '',

      uniteLocativeId: '',

      locataireId: '',

      dateDebut: null,

      dateFin: null,

      montantLoyer: null,

      montantCaution: 0,

      frequencePaiement: 1,

      delaiJoursTolerance: 5,

      statut: StatutContrat.Actif
    });

    this.contratDialog = true;
  }


editContrat(contrat: ContratDto): void {
  // ==========================================================
  // PERMISSION
  // ==========================================================

  if (!this.canManageContrat) {

    this.messageService.add({
      severity: 'warn',
      summary: 'Accès refusé',
      detail:
        'Vous n\'avez pas les droits nécessaires pour modifier un contrat.'
    });

    return;
  }

  // ==========================================================
  // VÉRIFICATION ID
  // ==========================================================

  if (!contrat.id) {

    this.messageService.add({
      severity: 'error',
      summary: 'Erreur',
      detail:
        'L\'identifiant du contrat est introuvable.'
    });

    return;
  }

  // ==========================================================
  // MODE MODIFICATION
  // ==========================================================

  this.isEditMode = true;
  this.selectedContratId = contrat.id;

  // ==========================================================
  // RÉCUPÉRATION DES IDs
  //
  // Structure réelle de l'API :
  //
  // contrat
  //   ├── bienImmobilier.id
  //   ├── uniteLocative.id
  //   └── locataire.id
  // ==========================================================

  const bienImmobilierId =
    contrat.bienImmobilier?.id ?? '';

  const uniteLocativeId =
    contrat.uniteLocative?.id
    ?? contrat.uniteLocativeId
    ?? '';

  const locataireId =
    contrat.locataire?.id
    ?? contrat.locataireId
    ?? '';

  // ==========================================================
  // RESET DES OPTIONS D'UNITÉS
  // ==========================================================

  this.unitesLocativesOptions = [];
  // ==========================================================
  // REMPLISSAGE DU FORMULAIRE
  // ==========================================================

  this.contratForm.patchValue({

    reference:
      contrat.reference ?? '',

    bienImmobilierId:
      bienImmobilierId,

    // L'unité sera sélectionnée après son chargement
    uniteLocativeId:
      '',

    locataireId:
      locataireId,

    dateDebut:
      contrat.dateDebut
        ? this.formatDateForInput(contrat.dateDebut)
        : null,

    dateFin:
      contrat.dateFin
        ? this.formatDateForInput(contrat.dateFin)
        : null,

    montantLoyer:
      contrat.montantLoyer ?? null,

    montantCaution:
      contrat.montantCaution ?? 0,

    frequencePaiement:
      contrat.frequencePaiement ?? 1,

    delaiJoursTolerance:
      contrat.delaiJoursTolerance ?? 5,

    statut:
      contrat.statut
  });


  // ==========================================================
  // CHARGEMENT DES UNITÉS DU BIEN
  // ==========================================================

  if (!bienImmobilierId) {

    console.error(
      'Le contrat ne contient pas de bien immobilier.',
      contrat
    );

    this.messageService.add({
      severity: 'warn',
      summary: 'Bien immobilier introuvable',
      detail:
        'Impossible de déterminer le bien immobilier associé au contrat.'
    });

    this.contratDialog = true;

    return;
  }


  this.unitesLocativesService
    .getUnitesParBien(bienImmobilierId)
    .subscribe({

      // ======================================================
      // UNITÉS CHARGÉES
      // ======================================================

      next: (
        unites: UniteLocativeDto[]
      ) => {

        if (!Array.isArray(unites)) {

          this.unitesLocativesOptions = [];

          this.contratDialog = true;

          return;
        }


        // ====================================================
        // CONSTRUCTION DES OPTIONS
        // ====================================================

        this.unitesLocativesOptions =
          unites.map(
            (unite: UniteLocativeDto) => ({

              label:
                unite.reference,

              value:
                unite.id
            })
          );


        // ====================================================
        // SÉLECTION DE L'UNITÉ DU CONTRAT
        // ====================================================

        const uniteExiste =
          this.unitesLocativesOptions.some(
            option =>
              option.value === uniteLocativeId
          );


        if (uniteExiste) {

          this.contratForm.patchValue({

            uniteLocativeId:
              uniteLocativeId

          });

        } else {

          console.warn(
            'L\'unité locative du contrat n\'est pas présente dans les unités du bien.',
            {
              uniteLocativeId,
              bienImmobilierId,
              options:
                this.unitesLocativesOptions
            }
          );

          this.contratForm.patchValue({

            uniteLocativeId:
              ''

          });
        }


        // ====================================================
        // OUVERTURE DU MODAL
        // ====================================================

        this.contratDialog = true;
      },


      // ======================================================
      // ERREUR
      // ======================================================

      error: (
        err: HttpErrorResponse
      ) => {

        console.error(
          'Erreur chargement unités du bien :',
          err
        );

        this.messageService.add({

          severity: 'error',

          summary: 'Erreur',

          detail:
            'Impossible de charger les unités locatives du bien.'
        });

        this.contratDialog = true;
      }
    });
}


  // ============================================================
  // SAUVEGARDE
  // ============================================================

  saveContrat(): void {

    if (!this.canManageContrat) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Accès refusé',

        detail:
          'Vous n\'avez pas les droits nécessaires pour gérer les contrats.'
      });

      return;
    }

    if (this.contratForm.invalid) {

      this.contratForm.markAllAsTouched();

      return;
    }

    const formValue =
      this.contratForm.getRawValue();


    // ==========================================================
    // MODIFICATION
    // ==========================================================

    if (this.isEditMode) {

      if (!this.selectedContratId) {

        this.messageService.add({

          severity: 'error',

          summary: 'Erreur',

          detail:
            'L\'identifiant du contrat est manquant pour la modification.'
        });

        return;
      }

      /*
       * IMPORTANT :
       *
       * La référence n'est PAS envoyée lors d'une modification.
       *
       * La référence est immutable après création.
       */
      const payload: ModifierContratRequest = {

        dateDebut:
          this.convertToIsoDate(
            formValue.dateDebut
          ),

        dateFin:
          this.convertToIsoDate(
            formValue.dateFin
          ),

        montantLoyer:
          Number(
            formValue.montantLoyer
          ),

        montantCaution:
          Number(
            formValue.montantCaution
          ),

        frequencePaiement:
          Number(
            formValue.frequencePaiement ?? 1
          ),

        delaiJoursTolerance:
          Number(
            formValue.delaiJoursTolerance ?? 0
          ),

        statut:
          Number(
            formValue.statut
          ) as StatutContrat
      };

      this.contratsService
        .updateContrat(
          this.selectedContratId,
          payload
        )
        .subscribe({

          next: () => {

            this.messageService.add({

              severity: 'success',

              summary: 'Succès',

              detail:
                'Contrat mis à jour.'
            });

            this.fermerDialog();

            this.chargerContrats();
          },

          error: (
            err: HttpErrorResponse
          ) => {

            this.traiterErreurHttp(
              err,
              'modification'
            );
          }
        });

      return;
    }


    // ==========================================================
    // CRÉATION
    // ==========================================================

    const payload: CreerContratRequest = {

      reference:
        String(
          formValue.reference ?? ''
        ).trim(),

      uniteLocativeId:
        formValue.uniteLocativeId,

      locataireId:
        formValue.locataireId,

      dateDebut:
        this.convertToIsoDate(
          formValue.dateDebut
        ),

      dateFin:
        this.convertToIsoDate(
          formValue.dateFin
        ),

      montantLoyer:
        Number(
          formValue.montantLoyer
        ),

      montantCaution:
        Number(
          formValue.montantCaution
        ),

      frequencePaiement:
        Number(
          formValue.frequencePaiement ?? 1
        ),

      delaiJoursTolerance:
        Number(
          formValue.delaiJoursTolerance ?? 0
        )
    };

    this.contratsService
      .createContrat(
        payload
      )
      .subscribe({

        next: () => {

          this.messageService.add({

            severity: 'success',

            summary: 'Succès',

            detail:
              'Contrat créé.'
          });

          this.fermerDialog();

          this.chargerContrats();
        },

        error: (
          err: HttpErrorResponse
        ) => {

          this.traiterErreurHttp(
            err,
            'création'
          );
        }
      });
  }


  // ============================================================
  // FERMETURE DU DIALOGUE
  // ============================================================

  fermerDialog(): void {

    this.contratDialog = false;

    this.isEditMode = false;

    this.selectedContratId = undefined;

    this.unitesLocativesOptions = [];
  }


  // ============================================================
  // SUPPRESSION
  // ============================================================

  deleteContrat(
    contrat: ContratDto
  ): void {

    if (!this.canManageContrat) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Accès refusé',

        detail:
          'Vous n\'avez pas les droits nécessaires pour supprimer un contrat.'
      });

      return;
    }

    if (!contrat.id) {
      return;
    }

    this.confirmationService.confirm({

      message:
        'Voulez-vous vraiment supprimer ce contrat ?',

      header:
        'Confirmation de suppression',

      icon:
        'pi pi-exclamation-triangle',

      acceptLabel:
        'Oui, supprimer',

      rejectLabel:
        'Annuler',

      acceptButtonStyleClass:
        'p-button-danger',

      accept: () => {

        this.contratsService
          .deleteContrat(
            contrat.id!
          )
          .subscribe({

            next: () => {

              this.messageService.add({

                severity: 'success',

                summary: 'Succès',

                detail:
                  'Contrat supprimé.'
              });

              this.chargerContrats();
            },

            error: (
              err: HttpErrorResponse
            ) => {

              this.traiterErreurHttp(
                err,
                'suppression'
              );
            }
          });
      }
    });
  }


  // ============================================================
  // PERMISSIONS
  // ============================================================

  private verifierPermissions(): void {

    this.canManageContrat =
      this.authService.hasRole([

        ROLES.Administrateur,

        ROLES.Admin,

        ROLES.Gestionnaire

      ]);
  }


  // ============================================================
  // DATES
  // ============================================================

  private formatDateForInput(
    date: string | Date
  ): string {

    const parsedDate =
      new Date(date);

    if (
      Number.isNaN(
        parsedDate.getTime()
      )
    ) {

      return '';
    }

    const year =
      parsedDate.getFullYear();

    const month =
      String(
        parsedDate.getMonth() + 1
      ).padStart(
        2,
        '0'
      );

    const day =
      String(
        parsedDate.getDate()
      ).padStart(
        2,
        '0'
      );

    return `${year}-${month}-${day}`;
  }


  private convertToIsoDate(
    date: string | Date
  ): string {

    const parsedDate =
      new Date(date);

    if (
      Number.isNaN(
        parsedDate.getTime()
      )
    ) {

      return '';
    }

    return parsedDate.toISOString();
  }


  // ============================================================
  // ERREURS HTTP
  // ============================================================

  private traiterErreurHttp(
    err: HttpErrorResponse,
    action:
      | 'chargement'
      | 'création'
      | 'modification'
      | 'suppression'
  ): void {

    console.error(
      `Erreur lors de la ${action} du contrat :`,
      err
    );

    if (err.status === 403) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Accès refusé',

        detail:
          `Vous n'avez pas les droits nécessaires pour la ${action} d'un contrat.`,

        life: 5000
      });

      return;
    }

    if (err.status === 401) {

      this.messageService.add({

        severity: 'error',

        summary: 'Non authentifié',

        detail:
          'Votre session a expiré. Veuillez vous reconnecter.',

        life: 5000
      });

      return;
    }

    const apiMessage =
      err.error?.message;

    if (
      err.status === 400 &&
      typeof apiMessage === 'string' &&
      apiMessage.trim()
    ) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Données invalides',

        detail:
          apiMessage,

        life: 5000
      });

      return;
    }

    this.messageService.add({

      severity: 'error',

      summary: 'Erreur',

      detail:
        `Une erreur est survenue lors de la ${action} du contrat ` +
        `(Code ${err.status || 'inconnu'}).`,

      life: 5000
    });
  }
}