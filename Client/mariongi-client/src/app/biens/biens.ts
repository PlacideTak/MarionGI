import {
  Component,
  inject,
  OnInit
} from '@angular/core';

import { CommonModule } from '@angular/common';

import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';

import {
  ActivatedRoute,
  RouterLink
} from '@angular/router';

import {
  MessageService,
  ConfirmationService
} from 'primeng/api';

import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';

import { BiensService } from '../services/biens.service';
import { AuthService } from '../login/auth.service';

import {
  BienDto,
  TypeBien,
  ROLES,
  CreerBienRequest,
  ModifierBienRequest
} from '../models/gestimmo.models';


@Component({
  selector: 'app-biens',
  standalone: true,

  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    SelectModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    ToastModule,
    ConfirmDialogModule,
    TagModule,
    TooltipModule
  ],

  providers: [
    MessageService,
    ConfirmationService
  ],

  templateUrl: './biens.html',
  styleUrls: ['./biens.scss']
})
export class Biens implements OnInit {

  // ============================================================
  // SERVICES
  // ============================================================

  readonly bienService = inject(BiensService);

  private readonly authService =
    inject(AuthService);

  private readonly fb =
    inject(FormBuilder);

  private readonly messageService =
    inject(MessageService);

  private readonly confirmationService =
    inject(ConfirmationService);

  private readonly route =
    inject(ActivatedRoute);


  // ============================================================
  // DONNÉES
  // ============================================================

  biens: BienDto[] = [];

  chargement = false;

  userRole = '';


  // ============================================================
  // MODALE
  // ============================================================

  displayModal = false;

  isEditMode = false;

  selectedBienId: string | null = null;

  bienForm!: FormGroup;


  // ============================================================
  // PHOTOS
  // ============================================================

  /**
   * Nouvelles photos sélectionnées par l'utilisateur.
   */
  fichiersSelectionnes: File[] = [];

  /**
   * Photos déjà enregistrées sur le serveur.
   *
   * En modification, seules les photos présentes dans cette liste
   * seront conservées.
   */
  photosExistantes: string[] = [];

  /**
   * Prévisualisations des nouvelles photos.
   */
  apercusPhotos: string[] = [];

  /**
   * Nombre maximum de photos par bien.
   */
  readonly maxPhotos = 20;

  /**
   * Taille maximale d'un fichier : 10 Mo.
   */
  readonly maxTaillePhoto = 10 * 1024 * 1024;

  /**
   * Extensions autorisées.
   */
  readonly extensionsPhotosAutorisees = [
    '.jpg',
    '.jpeg',
    '.png',
    '.webp'
  ];


  // ============================================================
  // DROITS
  // ============================================================

  get canManageBiens(): boolean {

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

    /*
     * Vérifie si la page a été ouverte avec :
     *
     * /biens?edit=ID_DU_BIEN
     *
     * Dans ce cas, le bien correspondant sera automatiquement
     * ouvert dans le popup de modification.
     */
    this.route.queryParamMap.subscribe(params => {

      const editId =
        params.get('edit');

      this.chargerBiens(editId);
    });
  }


  // ============================================================
  // FORMULAIRE
  // ============================================================

  initForm(): void {

    this.bienForm = this.fb.group({

      nom: [
        '',
        [
          Validators.required,
          Validators.maxLength(150)
        ]
      ],

      reference: [
        '',
        [
          Validators.required,
          Validators.maxLength(50)
        ]
      ],

      type: [
        TypeBien.Immeuble,
        Validators.required
      ],

      adresse: [
        '',
        [
          Validators.required,
          Validators.maxLength(250)
        ]
      ],

      ville: [
        '',
        [
          Validators.required,
          Validators.maxLength(100)
        ]
      ],

      quartier: [
        '',
        Validators.maxLength(100)
      ],

      superficie: [
        0,
        [
          Validators.required,
          Validators.min(1)
        ]
      ]
    });
  }


  // ============================================================
  // CHARGEMENT DES BIENS
  // ============================================================

  chargerBiens(
    editId: string | null = null
  ): void {

    this.chargement = true;

    this.bienService
      .getBiens()
      .subscribe({

        next: (data: BienDto[]) => {

          this.biens = data ?? [];

          this.chargement = false;


          // ====================================================
          // OUVERTURE AUTOMATIQUE DU POPUP DE MODIFICATION
          // ====================================================

          if (editId) {

            const bien =
              this.biens.find(
                b => b.id === editId
              );

            if (bien) {

              this.ouvrirModalModification(
                bien
              );

              /*
               * Supprime le paramètre "edit" de l'URL
               * après ouverture du popup.
               *
               * Cela évite qu'un simple rafraîchissement
               * de la page réouvre automatiquement le popup.
               */
              this.routerWithoutReload();

            } else {

              console.warn(
                `Le bien ${editId} n'a pas été trouvé.`
              );

              this.messageService.add({

                severity: 'warn',

                summary: 'Bien introuvable',

                detail:
                  'Le bien demandé n’existe plus ou n’est pas accessible.'
              });
            }
          }
        },

        error: (err) => {

          console.error(
            'Erreur chargement biens:',
            err
          );

          this.chargement = false;

          this.messageService.add({

            severity: 'error',

            summary: 'Erreur',

            detail:
              'Impossible de charger la liste des biens.'
          });
        }
      });
  }


  // ============================================================
  // NETTOYAGE DU PARAMÈTRE "edit"
  // ============================================================

  private routerWithoutReload(): void {

    /*
     * On retire uniquement le paramètre "edit"
     * sans recharger le composant.
     */
    window.history.replaceState(
      {},
      '',
      window.location.pathname
    );
  }


  // ============================================================
  // AJOUT D'UN BIEN
  // ============================================================

  ouvrirModalAjout(): void {

    if (!this.canManageBiens) {
      return;
    }

    this.isEditMode = false;

    this.selectedBienId = null;

    this.reinitialiserPhotos();

    this.bienForm.reset({

      nom: '',

      reference: '',

      type: TypeBien.Immeuble,

      adresse: '',

      ville: '',

      quartier: '',

      superficie: 0
    });

    this.displayModal = true;
  }


  // ============================================================
  // MODIFICATION D'UN BIEN
  // ============================================================

  ouvrirModalModification(
    bien: BienDto
  ): void {

    if (!this.canManageBiens) {
      return;
    }

    this.isEditMode = true;

    this.selectedBienId = bien.id;

    this.fichiersSelectionnes = [];

    this.apercusPhotos = [];

    this.photosExistantes = [
      ...(bien.photos ?? [])
    ];

    this.bienForm.patchValue({

      nom:
        bien.nom,

      reference:
        bien.reference,

      type:
        bien.type,

      adresse:
        bien.adresse,

      ville:
        bien.ville,

      quartier:
        bien.quartier ?? '',

      superficie:
        bien.superficie
    });

    this.displayModal = true;
  }


  // ============================================================
  // SÉLECTION DES PHOTOS
  // ============================================================

  selectionnerPhotos(
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

    const nombreActuel =
      this.photosExistantes.length +
      this.fichiersSelectionnes.length;

    const nombreDisponible =
      this.maxPhotos - nombreActuel;

    if (nombreDisponible <= 0) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Limite atteinte',

        detail:
          `Un bien ne peut pas contenir plus de ${this.maxPhotos} photos.`
      });

      input.value = '';

      return;
    }

    const fichiersAControler =
      fichiers.slice(
        0,
        nombreDisponible
      );

    if (
      fichiers.length >
      nombreDisponible
    ) {

      this.messageService.add({

        severity: 'warn',

        summary:
          'Certaines photos ignorées',

        detail:
          `Vous pouvez encore ajouter seulement ${nombreDisponible} photo(s).`
      });
    }

    for (
      const fichier of fichiersAControler
    ) {

      const validation =
        this.validerFichierPhoto(
          fichier
        );

      if (!validation.valide) {

        this.messageService.add({

          severity: 'warn',

          summary:
            'Photo non valide',

          detail:
            validation.message
        });

        continue;
      }

      this.fichiersSelectionnes.push(
        fichier
      );

      const reader =
        new FileReader();

      reader.onload = () => {

        if (
          typeof reader.result ===
          'string'
        ) {

          this.apercusPhotos.push(
            reader.result
          );
        }
      };

      reader.readAsDataURL(
        fichier
      );
    }

    /*
     * Permet de sélectionner à nouveau
     * le même fichier.
     */
    input.value = '';
  }


  // ============================================================
  // VALIDATION D'UN FICHIER
  // ============================================================

  private validerFichierPhoto(
    fichier: File
  ): {
    valide: boolean;
    message: string;
  } {

    if (fichier.size <= 0) {

      return {

        valide: false,

        message:
          `Le fichier "${fichier.name}" est vide.`
      };
    }

    if (
      fichier.size >
      this.maxTaillePhoto
    ) {

      return {

        valide: false,

        message:
          `La photo "${fichier.name}" dépasse la limite de 10 Mo.`
      };
    }

    const nomFichier =
      fichier.name.toLowerCase();

    const extension =
      nomFichier.substring(
        nomFichier.lastIndexOf('.')
      );

    if (
      !this.extensionsPhotosAutorisees
        .includes(extension)
    ) {

      return {

        valide: false,

        message:
          `Le format "${extension}" n'est pas autorisé.`
      };
    }

    return {

      valide: true,

      message: ''
    };
  }


  // ============================================================
  // SUPPRIMER UNE NOUVELLE PHOTO
  // ============================================================

  supprimerNouvellePhoto(
    index: number
  ): void {

    this.fichiersSelectionnes.splice(
      index,
      1
    );

    this.apercusPhotos.splice(
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

    this.photosExistantes.splice(
      index,
      1
    );
  }


  // ============================================================
  // URL D'UNE PHOTO
  // ============================================================

  getPhotoUrl(
    photo: string
  ): string {

    return this.bienService.getPhotoUrl(
      photo
    );
  }


  // ============================================================
  // RÉINITIALISER LES PHOTOS
  // ============================================================

  private reinitialiserPhotos(): void {

    this.fichiersSelectionnes = [];

    this.photosExistantes = [];

    this.apercusPhotos = [];
  }


  // ============================================================
  // SAUVEGARDE
  // ============================================================

  sauvegarder(): void {

    if (!this.canManageBiens) {
      return;
    }

    if (this.bienForm.invalid) {

      this.bienForm.markAllAsTouched();

      return;
    }

    const valeurs =
      this.bienForm.getRawValue();


    // ==========================================================
    // CONTRÔLE DU NOMBRE TOTAL DE PHOTOS
    // ==========================================================

    const nombreTotalPhotos =
      this.photosExistantes.length +
      this.fichiersSelectionnes.length;

    if (
      nombreTotalPhotos >
      this.maxPhotos
    ) {

      this.messageService.add({

        severity: 'warn',

        summary: 'Trop de photos',

        detail:
          `Un maximum de ${this.maxPhotos} photos est autorisé.`
      });

      return;
    }


    // ==========================================================
    // CRÉATION
    // ==========================================================

    const creation: CreerBienRequest = {

      nom:
        String(valeurs.nom).trim(),

      reference:
        String(valeurs.reference).trim(),

      type:
        valeurs.type,

      adresse:
        String(valeurs.adresse).trim(),

      ville:
        String(valeurs.ville).trim(),

      quartier:
        valeurs.quartier
          ? String(
              valeurs.quartier
            ).trim()
          : null,

      superficie:
        Number(
          valeurs.superficie
        ),

      fichiers:
        this.fichiersSelectionnes
    };


    // ==========================================================
    // MODIFICATION
    // ==========================================================

    if (
      this.isEditMode &&
      this.selectedBienId
    ) {

      const modification:
        ModifierBienRequest = {

        nom:
          creation.nom,

        reference:
          creation.reference,

        type:
          creation.type,

        adresse:
          creation.adresse,

        ville:
          creation.ville,

        quartier:
          creation.quartier,

        superficie:
          creation.superficie,

        photosExistantes:
          [
            ...this.photosExistantes
          ],

        fichiers:
          [
            ...this.fichiersSelectionnes
          ]
      };


      this.bienService
        .updateBien(
          this.selectedBienId,
          modification
        )
        .subscribe({

          next: () => {

            this.messageService.add({

              severity:
                'success',

              summary:
                'Succès',

              detail:
                'Bien modifié avec succès.'
            });

            this.displayModal = false;

            this.reinitialiserPhotos();

            this.chargerBiens();
          },

          error: (err) => {

            this.gererErreurSauvegarde(

              err,

              'Erreur lors de la modification.'
            );
          }
        });

      return;
    }


    // ==========================================================
    // CRÉATION
    // ==========================================================

    this.bienService
      .createBien(
        creation
      )
      .subscribe({

        next: () => {

          this.messageService.add({

            severity:
              'success',

            summary:
              'Succès',

            detail:
              'Bien créé avec succès.'
          });

          this.displayModal = false;

          this.reinitialiserPhotos();

          this.chargerBiens();
        },

        error: (err) => {

          this.gererErreurSauvegarde(

            err,

            'Erreur lors de la création.'
          );
        }
      });
  }


  // ============================================================
  // GESTION DES ERREURS DE SAUVEGARDE
  // ============================================================

  private gererErreurSauvegarde(
    err: any,
    messageDefaut: string
  ): void {

    console.error(
      'Erreur sauvegarde bien:',
      err
    );

    const errorMessage =
      typeof err.error === 'string'
        ? err.error
        : err.error?.message ||
          err.error?.title ||
          '';

    const messageNormalise =
      errorMessage.toLowerCase();


    // ==========================================================
    // RÉFÉRENCE OU NOM DÉJÀ UTILISÉ
    // ==========================================================

    if (
      err.status === 409
    ) {

      if (
        messageNormalise.includes(
          'nom'
        )
      ) {

        this.messageService.add({

          severity:
            'error',

          summary:
            'Nom déjà utilisé',

          detail:
            'Un bien possède déjà ce nom dans votre société. Veuillez en choisir un autre.'
        });

        return;
      }

      if (
        messageNormalise.includes(
          'référence'
        ) ||
        messageNormalise.includes(
          'reference'
        )
      ) {

        this.messageService.add({

          severity:
            'error',

          summary:
            'Référence existante',

          detail:
            'Un bien possède déjà cette référence. Veuillez en choisir une autre.'
        });

        return;
      }

      this.messageService.add({

        severity:
          'error',

        summary:
          'Doublon',

        detail:
          errorMessage ||
          'Le nom ou la référence existe déjà pour cette société.'
      });

      return;
    }


    this.messageService.add({

      severity:
        'error',

      summary:
        'Erreur',

      detail:
        errorMessage ||
        messageDefaut
    });
  }


  // ============================================================
  // SUPPRESSION / ARCHIVAGE
  // ============================================================

  supprimerBien(
    bien: BienDto
  ): void {

    if (!this.canManageBiens) {
      return;
    }

    this.confirmationService.confirm({

      message:
        `Voulez-vous vraiment archiver le bien « ${bien.nom} » (${bien.reference}) ?`,

      header:
        'Confirmation de suppression',

      icon:
        'pi pi-exclamation-triangle',

      acceptLabel:
        'Oui',

      rejectLabel:
        'Non',

      accept: () => {

        this.bienService
          .deleteBien(
            bien.id
          )
          .subscribe({

            next: () => {

              this.messageService.add({

                severity:
                  'success',

                summary:
                  'Succès',

                detail:
                  'Bien archivé avec succès.'
              });

              this.chargerBiens();
            },

            error: (err) => {

              console.error(
                'Erreur suppression:',
                err
              );

              const message =
                typeof err.error ===
                'string'
                  ? err.error
                  : err.error?.message ||
                    err.error?.title ||
                    'Erreur lors de la suppression.';

              this.messageService.add({

                severity:
                  err.status === 409
                    ? 'warn'
                    : 'error',

                summary:
                  err.status === 409
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
  // LIBELLÉ TYPE
  // ============================================================

  getTypeLibelle(
    type: TypeBien
  ): string {

    return this.bienService
      .getTypeLibelle(
        type
      );
  }
}