
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';

import {
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { finalize } from 'rxjs';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';

import {
  CategorieIncident,
  ImputabiliteIncident,
  PrioriteIncident,
  StatutIncident,
  TicketIncidentDto,
  TicketIncidentDetailDto,
  TicketPhotoDto,
  TypePhotoIncident,
  TypeUniteLocative,
  UniteLocativeDto,
  ModifierTicketIncidentRequest,
  CreerTicketIntervenantRequest
} from '../models/gestimmo.models';

import { TicketsService } from '../services/tickets.service';
import { UnitesLocativesService } from '../services/uniteslocatives.service';


interface Option<T> {
  label: string;
  value: T;
}

interface LocataireOption {
  label: string;
  value: string;
}

/** Données d'un intervenant telles que renvoyées par l'API. */
interface IntervenantTicket {
  id: string;
  nomArtisan: string;
  specialite: string;
  telephone: string;
  courriel?: string | null;
  montantDevis?: number | null;
  montantFacture?: number | null;
  dateIntervention?: string | null;
}

/** Données envoyées à l'API pour modifier un intervenant. */
interface ModifierIntervenantTicketRequest {
  nomArtisan: string;
  specialite: string;
  telephone: string;
  courriel: string;
  montantDevis: number | null;
  montantFacture: number | null;
  dateIntervention: string | null;
}

@Component({
  selector: 'app-tickets',
  standalone: true,
  templateUrl: './tickets.html',
  styleUrl: './tickets.scss',
  providers: [MessageService, ConfirmationService],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    ConfirmDialogModule,
    DialogModule,
    InputNumberModule,
    InputTextModule,
    SelectModule,
    TableModule,
    TagModule,
    TextareaModule,
    ToastModule,
    TooltipModule
  ]
})
export class Tickets implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly ticketsService = inject(TicketsService);
  private readonly unitesService = inject(UnitesLocativesService);
  private readonly messages = inject(MessageService);
  private readonly confirmation = inject(ConfirmationService);

  readonly StatutIncident = StatutIncident;
  readonly PrioriteIncident = PrioriteIncident;
  loadingLocataires = false;
  readonly nombreMaxPhotos = 20;
  readonly tailleMaxPhoto = 10 * 1024 * 1024;
  readonly dateMaximale = this.dateLocaleISO();

  tickets: TicketIncidentDto[] = [];
  ticketsAffiches: TicketIncidentDto[] = [];
  unites: UniteLocativeDto[] = [];
  locataires: LocataireOption[] = [];

  /** Photos déjà enregistrées sur le ticket. */
  photosExistantes: TicketPhotoDto[] = [];

  /** Nouveaux fichiers du formulaire de création/modification. */
  photos: File[] = [];

  /** Prévisualisations des nouveaux fichiers du formulaire. */
  apercusPhotos: string[] = [];

  /** Nouveaux fichiers ajoutés depuis la fenêtre de détail. */
  photosSupplementaires: File[] = [];

  /** Prévisualisations des photos ajoutées depuis le détail. */
  apercusPhotosSupplementaires: string[] = [];

  loading = false;
  loadingUnites = false;
  loadingDetail = false;
  saving = false;

  modalCreationVisible = false;
  modalDetailVisible = false;
  modalIntervenantVisible = false;
  modeEditionIntervenant = false;

  /** Identifiant de l'intervenant en cours de modification. */
  intervenantIdEdition: string | null = null;

  /** Popup de saisie du résultat de réparation avant résolution. */
  modalResolutionVisible = false;
  resultatReparationSaisi = '';
  ticketEnResolution: TicketIncidentDetailDto | null = null;

  modalPhotosVisible = false;
  modeEdition = false;

  ticketSelectionne: TicketIncidentDetailDto | null = null;
  ticketIdEdition: string | null = null;

  recherche = '';
  filtreStatut: StatutIncident | null = null;
  filtrePriorite: PrioriteIncident | null = null;
  filtreCategorie: CategorieIncident | null = null;

  typePhotoSupplementaire: TypePhotoIncident =
    TypePhotoIncident.AvantIntervention;

  readonly categoriesOptions: Option<CategorieIncident>[] = [
    { label: 'Plomberie', value: CategorieIncident.Plomberie },
    { label: 'Électricité', value: CategorieIncident.Electricite },
    { label: 'Maçonnerie', value: CategorieIncident.Maconnerie },
    { label: 'Sécurité', value: CategorieIncident.Securite },
    { label: 'Autre', value: CategorieIncident.Autre }
  ];

  readonly prioritesOptions: Option<PrioriteIncident>[] = [
    { label: 'Basse', value: PrioriteIncident.Basse },
    { label: 'Moyenne', value: PrioriteIncident.Moyenne },
    { label: 'Haute', value: PrioriteIncident.Haute },
    { label: 'Urgente', value: PrioriteIncident.Urgente }
  ];

  readonly statutsOptions: Option<StatutIncident>[] = [
    { label: 'Ouvert', value: StatutIncident.Ouvert },
    { label: 'En cours', value: StatutIncident.EnCours },
    {
      label: 'En attente de validation',
      value: StatutIncident.EnAttenteValidation
    },
    { label: 'Résolu', value: StatutIncident.Resolu },
    { label: 'Clôturé', value: StatutIncident.Cloture }
  ];

  readonly imputabilitesOptions: Option<ImputabiliteIncident>[] = [
    { label: 'Indéterminée', value: ImputabiliteIncident.Indeterminee },
    { label: 'Société', value: ImputabiliteIncident.Societe },
    { label: 'Locataire', value: ImputabiliteIncident.Locataire }
  ];

  readonly typesPhotosOptions: Option<TypePhotoIncident>[] = [
    {
      label: 'Avant intervention',
      value: TypePhotoIncident.AvantIntervention
    },
    {
      label: 'Après intervention',
      value: TypePhotoIncident.ApresIntervention
    }
  ];

  formulaire: FormGroup = this.fb.group({
    reference: ['', [Validators.required, Validators.maxLength(50)]],
    uniteLocativeId: ['', Validators.required],
    locataireId: [''],
    categorie: [CategorieIncident.Autre, Validators.required],
    description: ['', [Validators.required, Validators.maxLength(4000)]],
    priorite: [PrioriteIncident.Moyenne, Validators.required],
    dateincident: [this.dateLocaleISO(), Validators.required],
    coutEstime: [null],
    imputableA: [ImputabiliteIncident.Indeterminee],
    validationDevisRequise: [false],
    typePhoto: [TypePhotoIncident.AvantIntervention]
  });

  formulaireIntervenant: FormGroup = this.fb.group({
    nomArtisan: ['', [Validators.required, Validators.maxLength(150)]],
    specialite: ['', [Validators.required, Validators.maxLength(150)]],
    telephone: ['', [Validators.required, Validators.maxLength(40)]],
    courriel: [
      '',
      [Validators.required, Validators.email, Validators.maxLength(254)]
    ],
    montantDevis: [null],
    montantFacture: [null],
    dateIntervention: ['']
  });

  ngOnInit(): void {
    this.chargerUnites();
    this.chargerTickets();
  }

  /**
   * Libère les URL temporaires créées pour les prévisualisations.
   */
  ngOnDestroy(): void {
    this.libererApercus(this.apercusPhotos);
    this.libererApercus(this.apercusPhotosSupplementaires);
  }

  get unitesOptions(): Option<string>[] {
    return this.unites.map(unite => {
      const u = unite as any;
      const reference = u.reference ?? u.referenceUnite ?? '';
      const bien = u.bienNom ?? u.nomBienImmobilier ?? '';
      const label = [reference, bien].filter(Boolean).join(' — ');

      return {
        label: label || 'Unité locative',
        value: u.id
      };
    });
  }

  get locatairesOptions(): LocataireOption[] {
    return this.locataires;
  }

  /** Nombre de photos déjà enregistrées sur le ticket sélectionné. */
  get nombrePhotosExistantes(): number {
    return this.ticketSelectionne?.photos?.length ?? 0;
  }

  /** Nombre total de photos existantes et nouvelles dans le formulaire. */
  get nombreTotalPhotosFormulaire(): number {
    return (this.modeEdition ? this.photosExistantes.length : 0)
      + this.photos.length;
  }

  chargerTickets(): void {
    this.loading = true;

    this.ticketsService.obtenirTous()
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (resultat: TicketIncidentDto[]) => {
          this.tickets = resultat ?? [];
          this.appliquerFiltresLocaux();
        },
        error: (erreur: HttpErrorResponse) => {
          this.afficherErreur(erreur, 'Impossible de charger les tickets.');
        }
      });
  }

  chargerUnites(): void {
    this.loadingUnites = true;

    this.unitesService.getUnites()
      .pipe(finalize(() => this.loadingUnites = false))
      .subscribe({
        next: (resultat: UniteLocativeDto[]) => {
          this.unites = resultat ?? [];
        },
        error: (erreur: HttpErrorResponse) => {
          this.afficherErreur(
            erreur,
            'Impossible de charger les unités locatives.'
          );
        }
      });
  }

  /**
   * Charge les locataires associés à l'unité.
   * Le deuxième paramètre permet de conserver le locataire du ticket en édition.
   */

  
  
private requeteLocatairesUnite = 0;

chargerLocatairesParUnite(
  uniteLocativeId: string,
  locataireIdAConserver?: string
): void {
  this.locataires = [];

  if (!uniteLocativeId) {
    this.formulaire.patchValue({ locataireId: '' });
    this.loadingLocataires = false;
    return;
  }

  if (!locataireIdAConserver) {
    this.formulaire.patchValue({ locataireId: '' });
  }

  this.loadingLocataires = true;

  this.ticketsService
    .obtenirLocatairesParUnite(uniteLocativeId)
    .pipe(
      finalize(() => {
        this.loadingLocataires = false;
      })
    )
    .subscribe({
      next: (resultat: any[]) => {
        this.locataires = (resultat ?? []).map(locataire => {
          const nomComplet =
            `${locataire.prenom ?? locataire.prenomLocataire ?? ''} ${
              locataire.nom ?? locataire.nomLocataire ?? ''
            }`.trim();

          return {
            label:
              locataire.nomComplet ||
              locataire.nomAffichage ||
              nomComplet ||
              locataire.email ||
              'Locataire',
            value: String(locataire.id)
          };
        });

        if (locataireIdAConserver) {
          const locataireExiste = this.locataires.some(
            l => l.value.toLowerCase() ===
              locataireIdAConserver.toLowerCase()
          );

          this.formulaire.patchValue({
            locataireId: locataireExiste
              ? locataireIdAConserver
              : ''
          });
        }
      },
      error: (erreur: HttpErrorResponse) => {
        this.locataires = [];
        this.formulaire.patchValue({ locataireId: '' });

        this.afficherErreur(
          erreur,
          'Impossible de charger les locataires.'
        );
      }
    });
}

  appliquerFiltresLocaux(): void {
    const recherche = this.recherche.trim().toLocaleLowerCase();

    this.ticketsAffiches = this.tickets.filter(ticket => {
      const t = ticket as any;

      const texte = [
        t.reference,
        t.uniteReference,
        t.referenceUnite,
        t.bienNom,
        t.nomBienImmobilier,
        t.locataireNom,
        t.nomLocataire,
        t.prenomLocataire,
        t.description,
        this.libelleCategorie(t.categorie)
      ].filter(Boolean).join(' ').toLocaleLowerCase();

      const correspondRecherche = !recherche || texte.includes(recherche);
      const correspondStatut =
        !this.filtreStatut || t.statut === this.filtreStatut;
      const correspondPriorite =
        !this.filtrePriorite || t.priorite === this.filtrePriorite;
      const correspondCategorie =
        !this.filtreCategorie || t.categorie === this.filtreCategorie;

      return correspondRecherche
        && correspondStatut
        && correspondPriorite
        && correspondCategorie;
    });
  }

  rechercher(): void {
    this.appliquerFiltresLocaux();
  }

  changerFiltres(): void {
    this.appliquerFiltresLocaux();
  }

  reinitialiserFiltres(): void {
    this.recherche = '';
    this.filtreStatut = null;
    this.filtrePriorite = null;
    this.filtreCategorie = null;
    this.appliquerFiltresLocaux();
  }

  ouvrirCreation(): void {
    this.modeEdition = false;
    this.ticketIdEdition = null;
    this.nettoyerPhotos();

    this.formulaire.reset({
      reference: '',
      uniteLocativeId: '',
      locataireId: '',
      categorie: CategorieIncident.Autre,
      description: '',
      priorite: PrioriteIncident.Moyenne,
      dateincident: this.dateLocaleISO(),
      coutEstime: null,
      imputableA: ImputabiliteIncident.Indeterminee,
      validationDevisRequise: false,
      typePhoto: TypePhotoIncident.AvantIntervention
    });

    this.locataires = [];
    this.modalCreationVisible = true;
  }

  supprimerTicket(
    ticket: TicketIncidentDto | TicketIncidentDetailDto
  ): void {
    if (!ticket || this.saving) {
      return;
    }

    this.confirmation.confirm({
      header: 'Supprimer le ticket',
      message: 'Voulez-vous vraiment supprimer ce ticket ?',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Supprimer',
      rejectLabel: 'Annuler',
      accept: () => {
        this.saving = true;

        this.ticketsService.supprimer(ticket.id)
          .pipe(finalize(() => this.saving = false))
          .subscribe({
            next: () => {
              this.messages.add({
                severity: 'success',
                summary: 'Ticket supprimé',
                detail: 'Le ticket a été supprimé.'
              });

              this.ticketSelectionne = null;
              this.chargerTickets();
            },
            error: (erreur: HttpErrorResponse) => {
              this.afficherErreur(
                erreur,
                'Impossible de supprimer le ticket.'
              );
            }
          });
      }
    });
  }


ouvrirModification(
  ticket: TicketIncidentDto | TicketIncidentDetailDto
): void {
  const t = ticket as any;

  this.modeEdition = true;
  this.ticketIdEdition = t.id;

  this.nettoyerPhotos();
  this.ticketSelectionne = null;
  this.modalDetailVisible = false;
  this.modalCreationVisible = false;
  this.loadingDetail = true;

  this.ticketsService.obtenirParId(t.id)
    .pipe(
      finalize(() => {
        this.loadingDetail = false;
      })
    )
    .subscribe({
      next: (detail: TicketIncidentDetailDto) => {
        const d = detail as any;

        // Récupérer les photos renvoyées par l'API.
        const photosBrutes = d.photos ?? d.Photos ?? [];

        const photosChargees: TicketPhotoDto[] =
          Array.isArray(photosBrutes)
            ? photosBrutes
            : [];

        // Conserver les photos existantes.
        this.photosExistantes = photosChargees;

        this.ticketSelectionne = {
          ...detail,
          photos: photosChargees
        } as TicketIncidentDetailDto;


        // Remplir le formulaire.
        this.formulaire.reset({
          reference: d.reference ?? d.Reference ?? t.reference ?? '',
          uniteLocativeId:
            d.uniteLocativeId ?? d.UniteLocativeId ?? '',
          locataireId:
            d.locataireId ?? d.LocataireId ?? '',
          categorie:
            d.categorie ?? d.Categorie ?? CategorieIncident.Autre,
          description:
            d.description ?? d.Description ?? '',
          priorite:
            d.priorite ?? d.Priorite ?? PrioriteIncident.Moyenne,
          dateincident: this.formaterDatePourInput(
            d.dateincident ?? d.dateIncident ?? d.DateIncident
          ),
          coutEstime:
            d.coutEstime ?? d.CoutEstime ?? null,
          imputableA:
            d.imputableA
            ?? d.ImputableA
            ?? ImputabiliteIncident.Indeterminee,
          validationDevisRequise:
            d.validationDevisRequise
            ?? d.ValidationDevisRequise
            ?? false,
          typePhoto:
            d.typePhoto
            ?? d.TypePhoto
            ?? TypePhotoIncident.AvantIntervention
        });

        this.chargerLocatairesParUnite(
          d.uniteLocativeId ?? d.UniteLocativeId ?? '',
          d.locataireId ?? d.LocataireId ?? ''
        );

        // Ouvrir le formulaire une fois les données chargées.
        this.modalCreationVisible = true;
      },

      error: (erreur: HttpErrorResponse) => {
        this.afficherErreur(
          erreur,
          'Impossible de charger le ticket à modifier.'
        );
      }
    });
}

  fermerCreation(): void {
    this.modalCreationVisible = false;
    this.nettoyerPhotos();
    this.formulaire.markAsPristine();
    this.formulaire.markAsUntouched();
  }

  enregistrerTicket(): void {
    this.formulaire.markAllAsTouched();

    if (this.formulaire.invalid) {
      this.messages.add({
        severity: 'warn',
        summary: 'Formulaire incomplet',
        detail: 'Vérifie les champs obligatoires en rouge.'
      });
      return;
    }

    const v = this.formulaire.getRawValue();

    if (!this.dateValide(v.dateincident)) {
      this.formulaire.get('dateincident')?.setErrors({
        dateInvalide: true
      });

      this.messages.add({
        severity: 'error',
        summary: 'Date invalide',
        detail:
          'La date de l’incident doit être valide et ne pas dépasser aujourd’hui.'
      });
      return;
    }

    if (v.coutEstime !== null && Number(v.coutEstime) < 0) {
      this.messages.add({
        severity: 'error',
        summary: 'Coût invalide',
        detail: 'Le coût estimé ne peut pas être négatif.'
      });
      return;
    }

    const nombrePhotosExistantes = this.modeEdition
      ? this.photosExistantes.length
      : 0;

    const erreurPhotos = this.validerListePhotos(
      this.photos,
      nombrePhotosExistantes
    );

    if (erreurPhotos) {
      this.messages.add({
        severity: 'error',
        summary: 'Photos invalides',
        detail: erreurPhotos
      });
      return;
    }

    this.saving = true;

    const reference = String(v.reference ?? '').trim();

    if (this.modeEdition && this.ticketIdEdition) {
      const id = this.ticketIdEdition;
      const nouvellesPhotos = [...this.photos];
      const typePhoto = v.typePhoto as TypePhotoIncident;

      const demande = {
        reference,
        uniteLocativeId: v.uniteLocativeId,
        locataireId: v.locataireId || null,
        categorie: v.categorie,
        description: String(v.description ?? '').trim(),
        priorite: v.priorite,
        dateincident: v.dateincident,
        coutEstime:
          v.coutEstime === null || v.coutEstime === ''
            ? null
            : Number(v.coutEstime),
        imputableA: v.imputableA,
        validationDevisRequise: !!v.validationDevisRequise,
        typePhoto
      } as ModifierTicketIncidentRequest;

      this.ticketsService.modifier(id, demande)
        .subscribe({
          next: () => {
            // Si aucune nouvelle photo n'est sélectionnée,
            // la modification du ticket suffit.
            if (!nouvellesPhotos.length) {
              this.saving = false;

              this.messages.add({
                severity: 'success',
                summary: 'Ticket modifié',
                detail: 'Les modifications ont été enregistrées.'
              });

              this.modalCreationVisible = false;
              this.nettoyerPhotos();
              this.chargerTickets();
              this.actualiserTicketSelectionne(id);
              return;
            }

            // L'API utilise un endpoint distinct pour ajouter les photos.
            this.ticketsService.ajouterPhotos(
              id,
              nouvellesPhotos,
              typePhoto
            )
              .pipe(finalize(() => this.saving = false))
              .subscribe({
                next: () => {
                  this.messages.add({
                    severity: 'success',
                    summary: 'Ticket modifié',
                    detail:
                      'Le ticket et les nouvelles photos ont été enregistrés.'
                  });

                  this.modalCreationVisible = false;
                  this.nettoyerPhotos();
                  this.chargerTickets();
                  this.actualiserTicketSelectionne(id);
                },
                error: (erreur: HttpErrorResponse) => {
                  this.messages.add({
                    severity: 'warn',
                    summary: 'Ticket modifié, photos non envoyées',
                    detail:
                      'Le ticket a été modifié, mais l’envoi des nouvelles photos a échoué.'
                  });

                  this.afficherErreur(
                    erreur,
                    'Impossible d’ajouter les nouvelles photos.'
                  );

                  this.chargerTickets();
                  this.actualiserTicketSelectionne(id);
                }
              });
          },
          error: (erreur: HttpErrorResponse) => {
            this.saving = false;
            this.afficherErreur(
              erreur,
              'Impossible de modifier le ticket.'
            );
          }
        });

      return;
    }

    // Création du ticket : les photos sont envoyées avec le formulaire.
    const formData = new FormData();

    formData.append('Reference', reference);
    formData.append('UniteLocativeId', String(v.uniteLocativeId));
    formData.append('Categorie', String(v.categorie));
    formData.append('Description', String(v.description ?? '').trim());
    formData.append('Priorite', String(v.priorite));
    formData.append('DateIncident', String(v.dateincident));
    formData.append('ImputableA', String(v.imputableA));

    formData.append(
      'ValidationDevisRequise',
      String(!!v.validationDevisRequise)
    );

    formData.append('TypePhoto', String(v.typePhoto));

    if (v.locataireId) {
      formData.append('LocataireId', String(v.locataireId));
    }

    if (v.coutEstime !== null && v.coutEstime !== '') {
      formData.append('CoutEstime', String(v.coutEstime));
    }

    this.photos.forEach(photo => {
      formData.append('Photos', photo, photo.name);
    });

    this.ticketsService.creer(formData)
      .pipe(finalize(() => this.saving = false))
      .subscribe({
        next: () => {
          this.messages.add({
            severity: 'success',
            summary: 'Ticket créé',
            detail: 'Le ticket a été créé avec succès.'
          });

          this.modalCreationVisible = false;
          this.nettoyerPhotos();
          this.chargerTickets();
        },
        error: (erreur: HttpErrorResponse) => {
          this.afficherErreur(
            erreur,
            'Impossible de créer le ticket.'
          );
        }
      });
  }

  // ============================================================
  // Photos du formulaire de création/modification
  // ============================================================

  selectionnerPhotos(event: Event): void {
    const input = event.target as HTMLInputElement;
    const fichiers = Array.from(input.files ?? []);

    if (!fichiers.length) {
      return;
    }

    const erreur = this.validerListePhotos(
      [...this.photos, ...fichiers],
      this.modeEdition ? this.photosExistantes.length : 0
    );

    if (erreur) {
      this.messages.add({
        severity: 'error',
        summary: 'Photos non acceptées',
        detail: erreur
      });

      input.value = '';
      return;
    }

    for (const fichier of fichiers) {
      this.photos.push(fichier);
      this.apercusPhotos.push(URL.createObjectURL(fichier));
    }

    input.value = '';
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

supprimerPhoto(index: number): void {
  const apercu = this.apercusPhotos[index];

  if (apercu) {
    URL.revokeObjectURL(apercu);
  }

  this.apercusPhotos.splice(index, 1);
  this.photos.splice(index, 1);
}

  nettoyerPhotos(): void {
    this.libererApercus(this.apercusPhotos);

    this.photos = [];
    this.apercusPhotos = [];
    this.photosExistantes = [];
  }

  // ============================================================
  // Validation et libération des prévisualisations
  // ============================================================

  validerListePhotos(
    fichiers: File[],
    nombrePhotosExistantes = 0
  ): string | null {
    if (nombrePhotosExistantes + fichiers.length > this.nombreMaxPhotos) {
      return `Un maximum de ${this.nombreMaxPhotos} photos est autorisé par ticket.`;
    }

    const typesAcceptes = [
      'image/jpeg',
      'image/png',
      'image/webp'
    ];

    const extensionsAcceptees = [
      'jpg',
      'jpeg',
      'png',
      'webp'
    ];

    for (const fichier of fichiers) {
      const extension = fichier.name.split('.').pop()?.toLowerCase();

      if (
        !typesAcceptes.includes(fichier.type) &&
        !extensionsAcceptees.includes(extension ?? '')
      ) {
        return `Le fichier « ${fichier.name} » n'est pas un format accepté.`;
      }

      if (fichier.size > this.tailleMaxPhoto) {
        return `Le fichier « ${fichier.name} » dépasse la limite de 10 Mo.`;
      }
    }

    return null;
  }

  private libererApercus(apercus: string[]): void {
    for (const url of apercus) {
      if (url.startsWith('blob:')) {
        URL.revokeObjectURL(url);
      }
    }
  }

  // ============================================================
  // Détail et statut du ticket
  // ============================================================

  ouvrirDetail(ticket: TicketIncidentDto): void {
    this.modalDetailVisible = true;
    this.loadingDetail = true;
    this.ticketSelectionne = null;

    this.ticketsService.obtenirParId(ticket.id)
      .pipe(finalize(() => this.loadingDetail = false))
      .subscribe({
        next: (detail: TicketIncidentDetailDto) => {
          this.ticketSelectionne = detail;
        },
        error: (erreur: HttpErrorResponse) => {
          this.afficherErreur(
            erreur,
            'Impossible de charger le détail du ticket.'
          );
        }
      });
  }

  fermerDetail(): void {
    this.modalDetailVisible = false;
    this.ticketSelectionne = null;
  }

  changerStatutTicket(statut: StatutIncident): void {
    const ticket = this.ticketSelectionne;

    if (!ticket || this.saving) {
      return;
    }

    // La résolution nécessite une description détaillée dans un popup dédié.
    if (statut === StatutIncident.Resolu) {
      this.ticketEnResolution = ticket;
      this.resultatReparationSaisi =
        String((ticket as any).resultatReparation ?? '');
      this.modalResolutionVisible = true;
      return;
    }

    this.confirmation.confirm({
      header: 'Confirmer le changement',
      message: `Veux-tu changer le statut du ticket vers « ${this.libelleStatut(statut)} » ?`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Confirmer',
      rejectLabel: 'Annuler',
      accept: () => {
        this.saving = true;

        this.ticketsService.modifierStatut(ticket.id, {
          statut,
          resultatReparation: null
        })
          .pipe(finalize(() => this.saving = false))
          .subscribe({
            next: () => {
              this.messages.add({
                severity: 'success',
                summary: 'Statut modifié',
                detail: 'Le statut du ticket a été mis à jour.'
              });

              this.actualiserTicketSelectionne(ticket.id);
              this.chargerTickets();
            },
            error: (erreur: HttpErrorResponse) => {
              this.afficherErreur(
                erreur,
                'Impossible de modifier le statut.'
              );
            }
          });
      }
    });
  }

  /** Enregistre le résultat saisi dans le popup et passe le ticket à Résolu. */
  confirmerResolution(): void {
    const ticket = this.ticketEnResolution;
    const resultatReparation = this.resultatReparationSaisi.trim();

    if (!ticket || this.saving) {
      return;
    }

    if (!resultatReparation) {
      this.messages.add({
        severity: 'warn',
        summary: 'Résultat obligatoire',
        detail:
          'Décris le résultat de la réparation avant de résoudre le ticket.'
      });
      return;
    }

    this.saving = true;

    this.ticketsService.modifierStatut(ticket.id, {
      statut: StatutIncident.Resolu,
      resultatReparation
    })
      .pipe(finalize(() => this.saving = false))
      .subscribe({
        next: () => {
          this.modalResolutionVisible = false;
          this.ticketEnResolution = null;
          this.resultatReparationSaisi = '';

          this.messages.add({
            severity: 'success',
            summary: 'Ticket résolu',
            detail: 'Le résultat de la réparation a été enregistré.'
          });

          this.actualiserTicketSelectionne(ticket.id);
          this.chargerTickets();
        },
        error: (erreur: HttpErrorResponse) => {
          this.afficherErreur(
            erreur,
            'Impossible de confirmer la résolution du ticket.'
          );
        }
      });
  }

  /** Ferme le popup sans modifier le statut du ticket. */
  annulerResolution(): void {
    if (this.saving) {
      return;
    }

    this.modalResolutionVisible = false;
    this.ticketEnResolution = null;
    this.resultatReparationSaisi = '';
  }

  validerDevisSelectionne(): void {
    const ticket = this.ticketSelectionne;

    if (!ticket || this.saving) {
      return;
    }

    this.confirmation.confirm({
      header: 'Valider le devis',
      message: 'Confirmer la validation du devis pour ce ticket ?',
      icon: 'pi pi-check-circle',
      acceptLabel: 'Valider',
      rejectLabel: 'Annuler',
      accept: () => {
        this.saving = true;

        this.ticketsService.validerDevis(ticket.id, {
          devisValide: true
        })
          .pipe(finalize(() => this.saving = false))
          .subscribe({
            next: () => {
              this.messages.add({
                severity: 'success',
                summary: 'Devis validé',
                detail: 'Le devis a été validé.'
              });

              this.actualiserTicketSelectionne(ticket.id);
              this.chargerTickets();
            },
            error: (erreur: HttpErrorResponse) => {
              this.afficherErreur(
                erreur,
                'Impossible de valider le devis.'
              );
            }
          });
      }
    });
  }

  cloturerTicketSelectionne(): void {
    const ticket = this.ticketSelectionne;

    if (!ticket || this.saving) {
      return;
    }

    this.confirmation.confirm({
      header: 'Clôturer le ticket',
      message: 'Confirmer la clôture de ce ticket ?',
      icon: 'pi pi-lock',
      acceptLabel: 'Clôturer',
      rejectLabel: 'Annuler',
      accept: () => {
        this.saving = true;

        this.ticketsService.cloturer(ticket.id)
          .pipe(finalize(() => this.saving = false))
          .subscribe({
            next: () => {
              this.messages.add({
                severity: 'success',
                summary: 'Ticket clôturé',
                detail: 'Le ticket a été clôturé.'
              });

              this.actualiserTicketSelectionne(ticket.id);
              this.chargerTickets();
            },
            error: (erreur: HttpErrorResponse) => {
              this.afficherErreur(
                erreur,
                'Impossible de clôturer le ticket.'
              );
            }
          });
      }
    });
  }

  // ============================================================
  // Intervenants
  // ============================================================

  /** Ouvre le formulaire en mode ajout. */
  ouvrirAjoutIntervenant(): void {
    if (!this.ticketSelectionne || this.saving) {
      return;
    }

    this.modeEditionIntervenant = false;
    this.intervenantIdEdition = null;

    this.formulaireIntervenant.reset({
      nomArtisan: '',
      specialite: '',
      telephone: '',
      courriel: '',
      montantDevis: null,
      montantFacture: null,
      dateIntervention: ''
    });

    this.formulaireIntervenant.markAsPristine();
    this.formulaireIntervenant.markAsUntouched();

    this.modalIntervenantVisible = true;
  }

  /** Ouvre le formulaire prérempli en mode modification. */
  modifierIntervenant(intervenant: IntervenantTicket): void {
    if (!this.ticketSelectionne || !intervenant || this.saving) {
      return;
    }

    this.modeEditionIntervenant = true;
    this.intervenantIdEdition = intervenant.id;

    this.formulaireIntervenant.reset({
      nomArtisan: intervenant.nomArtisan ?? '',
      specialite: intervenant.specialite ?? '',
      telephone: intervenant.telephone ?? '',
      courriel: intervenant.courriel ?? '',
      montantDevis: intervenant.montantDevis ?? null,
      montantFacture: intervenant.montantFacture ?? null,
      dateIntervention: intervenant.dateIntervention
        ? String(intervenant.dateIntervention).substring(0, 10)
        : ''
    });

    this.formulaireIntervenant.markAsPristine();
    this.formulaireIntervenant.markAsUntouched();

    this.modalIntervenantVisible = true;
  }

  /** Ajoute ou modifie un intervenant selon le mode du formulaire. */
  enregistrerIntervenant(): void {
    this.formulaireIntervenant.markAllAsTouched();

    if (
      this.formulaireIntervenant.invalid ||
      !this.ticketSelectionne ||
      this.saving
    ) {
      return;
    }

    const v = this.formulaireIntervenant.getRawValue();

    const montantDevis =
      v.montantDevis === null || v.montantDevis === ''
        ? null
        : Number(v.montantDevis);

    const montantFacture =
      v.montantFacture === null || v.montantFacture === ''
        ? null
        : Number(v.montantFacture);

    if (
      (montantDevis !== null &&
        (!Number.isFinite(montantDevis) || montantDevis < 0)) ||
      (montantFacture !== null &&
        (!Number.isFinite(montantFacture) || montantFacture < 0))
    ) {
      this.messages.add({
        severity: 'warn',
        summary: 'Montant invalide',
        detail:
          'Les montants du devis et de la facture doivent être positifs ou nuls.'
      });
      return;
    }

    const demande: ModifierIntervenantTicketRequest = {
      nomArtisan: String(v.nomArtisan ?? '').trim(),
      specialite: String(v.specialite ?? '').trim(),
      telephone: String(v.telephone ?? '').trim(),
      courriel: String(v.courriel ?? '').trim(),
      montantDevis,
      montantFacture,
      dateIntervention: v.dateIntervention || null
    };

    if (
      !demande.nomArtisan ||
      !demande.specialite ||
      !demande.telephone ||
      !demande.courriel
    ) {
      this.messages.add({
        severity: 'warn',
        summary: 'Champs obligatoires',
        detail:
          'Le nom, la spécialité, le téléphone et le courriel sont obligatoires.'
      });
      return;
    }

    const ticketId = this.ticketSelectionne.id;
    const intervenantId = this.intervenantIdEdition;
    const estModification = this.modeEditionIntervenant;

    if (estModification && !intervenantId) {
      this.messages.add({
        severity: 'error',
        summary: 'Modification impossible',
        detail: 'L’identifiant de l’intervenant est manquant.'
      });
      return;
    }

    this.saving = true;

    const operation = estModification
      ? this.ticketsService.modifierIntervenant(
          ticketId,
          intervenantId!,
          demande
        )
      : this.ticketsService.ajouterIntervenant(
          ticketId,
          demande as CreerTicketIntervenantRequest
        );

    operation
      .pipe(finalize(() => this.saving = false))
      .subscribe({
        next: () => {
          this.messages.add({
            severity: 'success',
            summary: estModification
              ? 'Intervenant modifié'
              : 'Intervenant ajouté',
            detail: estModification
              ? 'Les modifications de l’intervenant ont été enregistrées.'
              : 'L’intervenant a été ajouté au ticket.'
          });

          this.modalIntervenantVisible = false;
          this.modeEditionIntervenant = false;
          this.intervenantIdEdition = null;

          this.actualiserTicketSelectionne(ticketId);
          this.chargerTickets();
        },
        error: (erreur: HttpErrorResponse) => {
          this.afficherErreur(
            erreur,
            estModification
              ? 'Impossible de modifier cet intervenant.'
              : 'Impossible d’ajouter cet intervenant.'
          );
        }
      });
  }

  /** Supprime logiquement un intervenant après confirmation. */
  supprimerIntervenant(intervenant: IntervenantTicket): void {
    const ticket = this.ticketSelectionne;

    if (!ticket || !intervenant || this.saving) {
      return;
    }

    this.confirmation.confirm({
      header: 'Supprimer l’intervenant',
      message:
        `Voulez-vous vraiment supprimer l’intervenant « ${intervenant.nomArtisan} » ?`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Supprimer',
      rejectLabel: 'Annuler',
      accept: () => {
        this.saving = true;

        this.ticketsService.supprimerIntervenant(
          ticket.id,
          intervenant.id
        )
          .pipe(finalize(() => this.saving = false))
          .subscribe({
            next: () => {
              this.messages.add({
                severity: 'success',
                summary: 'Intervenant supprimé',
                detail: 'L’intervenant a été supprimé du ticket.'
              });

              this.actualiserTicketSelectionne(ticket.id);
              this.chargerTickets();
            },
            error: (erreur: HttpErrorResponse) => {
              this.afficherErreur(
                erreur,
                'Impossible de supprimer cet intervenant.'
              );
            }
          });
      }
    });
  }

  // ============================================================
  // Ajout de photos depuis le détail du ticket
  // ============================================================

  ouvrirAjoutPhotos(): void {
    if (
      !this.ticketSelectionne ||
      this.ticketSelectionne.statut === StatutIncident.Cloture
    ) {
      return;
    }

    this.nettoyerPhotosSupplementaires();

    this.typePhotoSupplementaire =
      TypePhotoIncident.AvantIntervention;

    this.modalPhotosVisible = true;
  }

  selectionnerPhotosSupplementaires(event: Event): void {
    const input = event.target as HTMLInputElement;
    const fichiers = Array.from(input.files ?? []);

    if (!fichiers.length) {
      return;
    }

    const nombreExistant = this.ticketSelectionne?.photos?.length ?? 0;

    const erreur = this.validerListePhotos(
      [...this.photosSupplementaires, ...fichiers],
      nombreExistant
    );

    if (erreur) {
      this.messages.add({
        severity: 'error',
        summary: 'Photos non acceptées',
        detail: erreur
      });

      input.value = '';
      return;
    }

    for (const fichier of fichiers) {
      this.photosSupplementaires.push(fichier);
      this.apercusPhotosSupplementaires.push(
        URL.createObjectURL(fichier)
      );
    }

    input.value = '';
  }

  supprimerPhotoSupplementaire(index: number): void {
    if (
      index < 0 ||
      index >= this.photosSupplementaires.length
    ) {
      return;
    }

    const apercu = this.apercusPhotosSupplementaires[index];

    if (apercu?.startsWith('blob:')) {
      URL.revokeObjectURL(apercu);
    }

    this.photosSupplementaires.splice(index, 1);
    this.apercusPhotosSupplementaires.splice(index, 1);
  }

  nettoyerPhotosSupplementaires(): void {
    this.libererApercus(this.apercusPhotosSupplementaires);

    this.photosSupplementaires = [];
    this.apercusPhotosSupplementaires = [];
  }

  ajouterPhotosAuTicket(): void {
    const ticket = this.ticketSelectionne;

    if (!ticket || !this.photosSupplementaires.length || this.saving) {
      return;
    }

    if (ticket.statut === StatutIncident.Cloture) {
      this.messages.add({
        severity: 'warn',
        summary: 'Ticket clôturé',
        detail: 'Il est impossible d’ajouter des photos à un ticket clôturé.'
      });
      return;
    }

    const erreur = this.validerListePhotos(
      this.photosSupplementaires,
      ticket.photos?.length ?? 0
    );

    if (erreur) {
      this.messages.add({
        severity: 'error',
        summary: 'Photos invalides',
        detail: erreur
      });
      return;
    }

    const id = ticket.id;
    const fichiers = [...this.photosSupplementaires];
    const typePhoto = this.typePhotoSupplementaire;

    this.saving = true;

    this.ticketsService.ajouterPhotos(
      id,
      fichiers,
      typePhoto
    )
      .pipe(finalize(() => this.saving = false))
      .subscribe({
        next: () => {
          this.messages.add({
            severity: 'success',
            summary: 'Photos ajoutées',
            detail: 'Les photos ont été envoyées.'
          });

          this.modalPhotosVisible = false;
          this.nettoyerPhotosSupplementaires();

          this.actualiserTicketSelectionne(id);
          this.chargerTickets();
        },
        error: (erreur: HttpErrorResponse) => {
          this.afficherErreur(
            erreur,
            'Impossible d’ajouter les photos.'
          );
        }
      });
  }

  // ============================================================
  // Actualisation, libellés et utilitaires
  // ============================================================

  actualiserTicketSelectionne(id: string): void {
    this.ticketsService.obtenirParId(id).subscribe({
      next: (detail: TicketIncidentDetailDto) => {
        this.ticketSelectionne = detail;
      },
      error: (erreur: HttpErrorResponse) => {
        this.afficherErreur(
          erreur,
          'Impossible d’actualiser le ticket.'
        );
      }
    });
  }

  libelleStatut(statut: StatutIncident | string): string {
    const valeurs: Record<string, string> = {
      [StatutIncident.Ouvert]: 'Ouvert',
      [StatutIncident.EnCours]: 'En cours',
      [StatutIncident.EnAttenteValidation]: 'En attente de validation',
      [StatutIncident.Resolu]: 'Résolu',
      [StatutIncident.Cloture]: 'Clôturé'
    };

    return valeurs[String(statut)] ?? String(statut ?? '—');
  }

  libellePriorite(priorite: PrioriteIncident | string): string {
    const valeurs: Record<string, string> = {
      [PrioriteIncident.Basse]: 'Basse',
      [PrioriteIncident.Moyenne]: 'Moyenne',
      [PrioriteIncident.Haute]: 'Haute',
      [PrioriteIncident.Urgente]: 'Urgente'
    };

    return valeurs[String(priorite)] ?? String(priorite ?? '—');
  }

  libelleCategorie(categorie: CategorieIncident | string): string {
    const valeurs: Record<string, string> = {
      [CategorieIncident.Plomberie]: 'Plomberie',
      [CategorieIncident.Electricite]: 'Électricité',
      [CategorieIncident.Maconnerie]: 'Maçonnerie',
      [CategorieIncident.Securite]: 'Sécurité',
      [CategorieIncident.Autre]: 'Autre'
    };

    return valeurs[String(categorie)] ?? String(categorie ?? '—');
  }

  libelleImputabilite(
    imputabilite: ImputabiliteIncident | string
  ): string {
    const valeurs: Record<string, string> = {
      [ImputabiliteIncident.Indeterminee]: 'Indéterminée',
      [ImputabiliteIncident.Societe]: 'Société',
      [ImputabiliteIncident.Locataire]: 'Locataire'
    };

    return valeurs[String(imputabilite)] ?? String(imputabilite ?? '—');
  }

  libelleTypeUnite(type: TypeUniteLocative | string): string {
    return String(type ?? 'Unité locative');
  }

  severiteStatut(
    statut: StatutIncident | string
  ): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (statut) {
      case StatutIncident.Ouvert:
        return 'info';
      case StatutIncident.EnCours:
        return 'warn';
      case StatutIncident.EnAttenteValidation:
        return 'secondary';
      case StatutIncident.Resolu:
        return 'success';
      case StatutIncident.Cloture:
        return 'secondary';
      default:
        return 'info';
    }
  }

  severitePriorite(
    priorite: PrioriteIncident | string
  ): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (priorite) {
      case PrioriteIncident.Basse:
        return 'success';
      case PrioriteIncident.Moyenne:
        return 'info';
      case PrioriteIncident.Haute:
        return 'warn';
      case PrioriteIncident.Urgente:
        return 'danger';
      default:
        return 'info';
    }
  }

  getPhotoUrl(url: string | null | undefined): string {
    if (!url) {
      return '';
    }

    return this.ticketsService.getPhotoUrl(url);
  }

  libelleTypePhoto(type: TypePhotoIncident | string): string {
    if (type === TypePhotoIncident.AvantIntervention) {
      return 'Avant intervention';
    }

    if (type === TypePhotoIncident.ApresIntervention) {
      return 'Après intervention';
    }

    return String(type ?? 'Photo');
  }

  champInvalide(nom: string): boolean {
    const controle = this.formulaire.get(nom);

    return !!controle
      && controle.invalid
      && (controle.touched || controle.dirty);
  }

  messageErreurChamp(nom: string): string {
    const controle = this.formulaire.get(nom);

    if (!controle?.errors) {
      return '';
    }

    if (controle.errors['required']) {
      return 'Ce champ est obligatoire.';
    }

    if (controle.errors['maxlength']) {
      return 'La valeur saisie est trop longue.';
    }

    if (controle.errors['dateInvalide']) {
      return 'La date saisie est invalide.';
    }

    return 'Valeur invalide.';
  }

  dateValide(valeur: string | null | undefined): boolean {
    if (!valeur) {
      return false;
    }

    const date = new Date(`${valeur}T00:00:00`);
    const aujourdHui = new Date(`${this.dateLocaleISO()}T00:00:00`);

    return !Number.isNaN(date.getTime()) && date <= aujourdHui;
  }

  formaterDatePourInput(
    valeur: string | Date | null | undefined
  ): string {
    if (!valeur) {
      return this.dateLocaleISO();
    }

    if (typeof valeur === 'string') {
      return valeur.length >= 10
        ? valeur.substring(0, 10)
        : valeur;
    }

    return this.dateLocaleISO(valeur);
  }

  compterStatut(statut: StatutIncident): number {
    return this.tickets.filter(ticket =>
      (ticket as any).statut === statut
    ).length;
  }

  dateTicket(ticket: TicketIncidentDto): string | Date | null {
    const t = ticket as any;

    return t.dateincident ?? t.dateIncident ?? null;
  }

  private dateLocaleISO(date = new Date()): string {
    const annee = date.getFullYear();
    const mois = String(date.getMonth() + 1).padStart(2, '0');
    const jour = String(date.getDate()).padStart(2, '0');

    return `${annee}-${mois}-${jour}`;
  }

  private afficherErreur(
    erreur: HttpErrorResponse,
    messageParDefaut: string
  ): void {
    const detail =
      erreur?.error?.message
      ?? erreur?.error?.detail
      ?? erreur?.error?.title
      ?? messageParDefaut;

    this.messages.add({
      severity: 'error',
      summary: 'Erreur',
      detail: String(detail)
    });
  }
}
