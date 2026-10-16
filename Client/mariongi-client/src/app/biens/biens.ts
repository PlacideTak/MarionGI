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
  ModifierBienRequest,
  StatutFoncier
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

  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly route = inject(ActivatedRoute);

  // ============================================================
  // STATUTS FONCIERS
  // Les valeurs doivent correspondre à l'enum C#.
  // ============================================================

  readonly statutsFonciers = [
    {
      label: 'Non renseigné',
      value: StatutFoncier.NonRenseigne
    },
    {
      label: 'Titre foncier disponible',
      value: StatutFoncier.TitreFoncierDisponible
    },
    {
      label: 'Demande en cours',
      value: StatutFoncier.DemandeEnCours
    },
    {
      label: 'Sans titre foncier',
      value: StatutFoncier.SansTitreFoncier
    },
    {
      label: 'Autre',
      value: StatutFoncier.Autre
    }
  ];

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

  fichiersSelectionnes: File[] = [];
  photosExistantes: string[] = [];
  apercusPhotos: string[] = [];

  readonly maxPhotos = 20;
  readonly maxTaillePhoto = 10 * 1024 * 1024;

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

    this.route.queryParamMap.subscribe(params => {
      const editId = params.get('edit');
      this.chargerBiens(editId);
    });
  }

  // ============================================================
  // FORMULAIRE
  // ============================================================

  initForm(): void {
    this.bienForm = this.fb.group({
      // Identification
      nom: ['', [
        Validators.required,
        Validators.maxLength(150)
      ]],
      reference: ['', [
        Validators.required,
        Validators.maxLength(50)
      ]],
      type: [
        TypeBien.Immeuble,
        Validators.required
      ],

      // Localisation
      adresse: ['', [
        Validators.required,
        Validators.maxLength(250)
      ]],
      ville: ['', [
        Validators.required,
        Validators.maxLength(100)
      ]],
      quartier: ['', Validators.maxLength(100)],
      region: [''],
      departement: [''],
      arrondissement: [''],

      // Caractéristiques
      superficie: [null, [
        Validators.required,
        Validators.min(1),
        Validators.max(1000000)
      ]],
      anneeConstruction: [null, [
        Validators.min(1),
        Validators.max(9999)
      ]],
      nombreEtages: [null, [
        Validators.min(0),
        Validators.max(1000)
      ]],

      // Informations cadastrales
      numeroTitreFoncier: [''],
      numeroLot: [''],
      numeroParcelle: [''],
      referenceCadastrale: [''],

      // Informations financières
      valeurAcquisition: [null, Validators.min(0)],
      dateAcquisition: [null],
      valeurEstimee: [null, Validators.min(0)],
      dateDerniereEvaluation: [null],

      // Statut foncier
      statutFoncier: [
        StatutFoncier.NonRenseigne
      ]
    });
  }

  private nombreOptionnel(
    valeur: unknown
  ): number | null {
    if (
      valeur === null ||
      valeur === undefined ||
      valeur === ''
    ) {
      return null;
    }

    const nombre = Number(valeur);

    return Number.isFinite(nombre)
      ? nombre
      : null;
  }

  private texteOptionnel(
    valeur: unknown
  ): string | null {
    if (
      valeur === null ||
      valeur === undefined
    ) {
      return null;
    }

    const texte = String(valeur).trim();

    return texte.length > 0
      ? texte
      : null;
  }

  // ============================================================
  // CHARGEMENT DES BIENS
  // ============================================================

  chargerBiens(
    editId: string | null = null
  ): void {
    this.chargement = true;

    this.bienService.getBiens().subscribe({
      next: (data: BienDto[]) => {
        this.biens = data ?? [];
        this.chargement = false;

        if (editId) {
          const bien = this.biens.find(
            b => b.id === editId
          );

          if (bien) {
            this.ouvrirModalModification(bien);
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
  // NETTOYAGE DU PARAMÈTRE EDIT
  // ============================================================

  private routerWithoutReload(): void {
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
      region: '',
      departement: '',
      arrondissement: '',

      superficie: null,
      anneeConstruction: null,
      nombreEtages: null,

      numeroTitreFoncier: '',
      numeroLot: '',
      numeroParcelle: '',
      referenceCadastrale: '',

      valeurAcquisition: null,
      dateAcquisition: null,
      valeurEstimee: null,
      dateDerniereEvaluation: null,

      statutFoncier: StatutFoncier.NonRenseigne
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
      nom: bien.nom,
      reference: bien.reference,
      type: bien.type,

      adresse: bien.adresse,
      ville: bien.ville,
      quartier: bien.quartier ?? '',
      region: bien.region ?? '',
      departement: bien.departement ?? '',
      arrondissement: bien.arrondissement ?? '',

      superficie: bien.superficie,
      anneeConstruction:
        bien.anneeConstruction ?? null,
      nombreEtages:
        bien.nombreEtages ?? null,

      numeroTitreFoncier:
        bien.numeroTitreFoncier ?? '',
      numeroLot: bien.numeroLot ?? '',
      numeroParcelle: bien.numeroParcelle ?? '',
      referenceCadastrale:
        bien.referenceCadastrale ?? '',

      valeurAcquisition:
        bien.valeurAcquisition ?? null,
      dateAcquisition:
        bien.dateAcquisition
          ? bien.dateAcquisition.substring(0, 10)
          : null,

      valeurEstimee:
        bien.valeurEstimee ?? null,
      dateDerniereEvaluation:
        bien.dateDerniereEvaluation
          ? bien.dateDerniereEvaluation.substring(0, 10)
          : null,

      statutFoncier:
        bien.statutFoncier ??
        StatutFoncier.NonRenseigne
    });

    this.displayModal = true;
  }

  // ============================================================
  // SÉLECTION DES PHOTOS
  // ============================================================

  selectionnerPhotos(event: Event): void {
    const input = event.target as HTMLInputElement;

    if (!input.files || input.files.length === 0) {
      return;
    }

    const fichiers = Array.from(input.files);

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
      fichiers.slice(0, nombreDisponible);

    if (fichiers.length > nombreDisponible) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Certaines photos ignorées',
        detail:
          `Vous pouvez encore ajouter seulement ${nombreDisponible} photo(s).`
      });
    }

    for (const fichier of fichiersAControler) {
      const validation =
        this.validerFichierPhoto(fichier);

      if (!validation.valide) {
        this.messageService.add({
          severity: 'warn',
          summary: 'Photo non valide',
          detail: validation.message
        });

        continue;
      }

      this.fichiersSelectionnes.push(fichier);

      const reader = new FileReader();

      reader.onload = () => {
        if (typeof reader.result === 'string') {
          this.apercusPhotos.push(reader.result);
        }
      };

      reader.readAsDataURL(fichier);
    }

    input.value = '';
  }

  // ============================================================
  // VALIDATION D'UN FICHIER PHOTO
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

    if (fichier.size > this.maxTaillePhoto) {
      return {
        valide: false,
        message:
          `La photo "${fichier.name}" dépasse la limite de 10 Mo.`
      };
    }

    const nomFichier = fichier.name.toLowerCase();

    const extension = nomFichier.substring(
      nomFichier.lastIndexOf('.')
    );

    if (
      !this.extensionsPhotosAutorisees.includes(extension)
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

  supprimerNouvellePhoto(index: number): void {
    this.fichiersSelectionnes.splice(index, 1);
    this.apercusPhotos.splice(index, 1);
  }

  // ============================================================
  // SUPPRIMER UNE PHOTO EXISTANTE
  // ============================================================

  supprimerPhotoExistante(index: number): void {
    this.photosExistantes.splice(index, 1);
  }

  // ============================================================
  // URL D'UNE PHOTO
  // ============================================================

  getPhotoUrl(photo: string): string {
    return this.bienService.getPhotoUrl(photo);
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

    const valeurs = this.bienForm.getRawValue();

    // Contrôle du nombre total de photos
    const nombreTotalPhotos =
      this.photosExistantes.length +
      this.fichiersSelectionnes.length;

    if (nombreTotalPhotos > this.maxPhotos) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Trop de photos',
        detail:
          `Un maximum de ${this.maxPhotos} photos est autorisé.`
      });

      return;
    }

    // Création du modèle commun
    const creation: CreerBienRequest = {
      nom: String(valeurs.nom).trim(),
      reference: String(valeurs.reference).trim(),
      type: valeurs.type as TypeBien,

      adresse: String(valeurs.adresse).trim(),
      ville: String(valeurs.ville).trim(),
      quartier: this.texteOptionnel(valeurs.quartier),
      region: this.texteOptionnel(valeurs.region),
      departement: this.texteOptionnel(valeurs.departement),
      arrondissement: this.texteOptionnel(valeurs.arrondissement),

      superficie: Number(valeurs.superficie),
      anneeConstruction:
        this.nombreOptionnel(valeurs.anneeConstruction),
      nombreEtages:
        this.nombreOptionnel(valeurs.nombreEtages),

      numeroTitreFoncier:
        this.texteOptionnel(valeurs.numeroTitreFoncier),
      numeroLot:
        this.texteOptionnel(valeurs.numeroLot),
      numeroParcelle:
        this.texteOptionnel(valeurs.numeroParcelle),
      referenceCadastrale:
        this.texteOptionnel(valeurs.referenceCadastrale),

      valeurAcquisition:
        this.nombreOptionnel(valeurs.valeurAcquisition),
      dateAcquisition:
        this.texteOptionnel(valeurs.dateAcquisition),
      valeurEstimee:
        this.nombreOptionnel(valeurs.valeurEstimee),
      dateDerniereEvaluation:
        this.texteOptionnel(valeurs.dateDerniereEvaluation),

      statutFoncier: Number(valeurs.statutFoncier),

      fichiers: [...this.fichiersSelectionnes]
    };

    // Modification
    if (this.isEditMode && this.selectedBienId) {
      const modification: ModifierBienRequest = {
        ...creation,
        photosExistantes: [...this.photosExistantes],
        fichiers: [...this.fichiersSelectionnes]
      };

      this.bienService
        .updateBien(this.selectedBienId, modification)
        .subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Succès',
              detail: 'Bien modifié avec succès.'
            });

            this.displayModal = false;
            this.reinitialiserPhotos();
            this.chargerBiens();
          },

          error: (err) => {
            this.gererErreurSauvegarde(err);
          }
        });

      return;
    }

    // Création
    this.bienService.createBien(creation).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Succès',
          detail: 'Bien créé avec succès.'
        });

        this.displayModal = false;
        this.reinitialiserPhotos();
        this.chargerBiens();
      },

      error: (err) => {
        this.gererErreurSauvegarde(err);
      }
    });
  }

  // ============================================================
  // GESTION DES ERREURS DE SAUVEGARDE
  // ============================================================

  private gererErreurSauvegarde(erreur: any): void {
    console.error(
      'Erreur lors de la sauvegarde du bien :',
      erreur
    );

    console.error(
      'Détails de validation :',
      erreur?.error?.errors
    );

    const erreursValidation = erreur?.error?.errors;

    if (erreursValidation) {
      const details = Object.entries(erreursValidation)
        .map(([champ, messages]) => {
          const listeMessages = Array.isArray(messages)
            ? messages.join(', ')
            : String(messages);

          return `${champ} : ${listeMessages}`;
        })
        .join('\n');

      this.messageService.add({
        severity: 'error',
        summary: 'Erreur de validation',
        detail: details,
        life: 10000
      });

      return;
    }

    this.messageService.add({
      severity: 'error',
      summary: 'Erreur',
      detail:
        erreur?.error?.detail ??
        erreur?.error?.title ??
        erreur?.message ??
        'Une erreur est survenue lors de la sauvegarde du bien.',
      life: 10000
    });
  }

  // ============================================================
  // SUPPRESSION / ARCHIVAGE
  // ============================================================

  supprimerBien(bien: BienDto): void {
    if (!this.canManageBiens) {
      return;
    }

    this.confirmationService.confirm({
      message:
        `Voulez-vous vraiment archiver le bien « ${bien.nom} » (${bien.reference}) ?`,

      header: 'Confirmation de suppression',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Oui',
      rejectLabel: 'Non',

      accept: () => {
        this.bienService.deleteBien(bien.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Succès',
              detail: 'Bien archivé avec succès.'
            });

            this.chargerBiens();
          },

          error: (err) => {
            console.error('Erreur suppression:', err);

            const message =
              typeof err.error === 'string'
                ? err.error
                : err.error?.message ??
                  err.error?.title ??
                  'Erreur lors de la suppression.';

            this.messageService.add({
              severity: err.status === 409 ? 'warn' : 'error',
              summary:
                err.status === 409
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
  // INDICATEURS DE SYNTHÈSE
  // ============================================================

  get nombreUnitesTotal(): number {
    return this.biens.reduce(
      (total, bien) =>
        total + (bien.nombreUnites ?? 0),
      0
    );
  }

  get nombreVilles(): number {
    const villes = this.biens
      .map(bien =>
        (bien.ville ?? '').trim().toLocaleLowerCase()
      )
      .filter(ville => ville.length > 0);

    return new Set(villes).size;
  }

  get nombrePhotosTotal(): number {
    return this.biens.reduce(
      (total, bien) =>
        total + (bien.photos?.length ?? 0),
      0
    );
  }

  // ============================================================
  // LIBELLÉ DU TYPE
  // ============================================================

  getTypeLibelle(type: TypeBien): string {
    return this.bienService.getTypeLibelle(type);
  }
}