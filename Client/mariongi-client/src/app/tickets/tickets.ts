
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
  BienDto,
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
  CreerTicketIntervenantRequest,
  ModifierTicketIntervenantRequest,
  PrestataireDto
} from '../models/gestimmo.models';

import { BiensService } from '../services/biens.service';
import { TicketsService } from '../services/tickets.service';
import { UnitesLocativesService } from '../services/uniteslocatives.service';
import { PrestatairesService } from '../services/prestataire.service';

interface Option<T> {
  label: string;
  value: T;
}

interface LocataireOption {
  label: string;
  value: string;
}

interface IntervenantTicket {
  id: string;
  prestataireId: string;
  nomPrestataire: string;
  typePrestataire?: number;
  specialite: string;
  telephone: string;
  courriel?: string | null;
  montantDevis?: number | null;
  montantFacture?: number | null;
  referenceDevis?: string | null;
  referenceFacture?: string | null;
  dateAssignation?: string | null;
  dateIntervention?: string | null;
  dateFinIntervention?: string | null;
  observations?: string | null;
}

interface PrestataireOption {
  label: string;
  value: string;
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
  private readonly biensService = inject(BiensService);
  private readonly ticketsService = inject(TicketsService);
  private readonly unitesService = inject(UnitesLocativesService);
  private readonly messages = inject(MessageService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly prestatairesService =   inject(PrestatairesService);

  readonly StatutIncident = StatutIncident;
  readonly PrioriteIncident = PrioriteIncident;

  readonly nombreMaxPhotos = 20;
  readonly tailleMaxPhoto = 10 * 1024 * 1024;
  readonly dateMaximale = this.dateLocaleISO();

  prestatairesOptions: PrestataireOption[] = [];
  loadingPrestataires = false;

  tickets: TicketIncidentDto[] = [];
  ticketsAffiches: TicketIncidentDto[] = [];

  biens: BienDto[] = [];
  biensOptions: Option<string>[] = [];

  unites: UniteLocativeDto[] = [];
  locataires: LocataireOption[] = [];

  photosExistantes: TicketPhotoDto[] = [];
  photos: File[] = [];
  apercusPhotos: string[] = [];

  photosSupplementaires: File[] = [];
  apercusPhotosSupplementaires: string[] = [];

  loading = false;
  loadingBiens = false;
  loadingUnites = false;
  loadingLocataires = false;
  loadingDetail = false;
  saving = false;

  modalCreationVisible = false;
  modalDetailVisible = false;
  modalIntervenantVisible = false;
  modeEditionIntervenant = false;

  intervenantIdEdition: string | null = null;

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

  private requeteLocatairesUnite = 0;
  private requeteUnitesBien = 0;

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
    bienImmobilierId: ['', Validators.required],
    uniteLocativeId: [''],
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

formulaireIntervenant: FormGroup = this.fb.group(
  {
    prestataireId: ['', Validators.required],

    montantDevis: [null],
    referenceDevis: ['', Validators.maxLength(100)],

    montantFacture: [null],
    referenceFacture: ['', Validators.maxLength(100)],

    dateAssignation: [''],
    dateIntervention: [''],
    dateFinIntervention: [''],

    observations: ['', Validators.maxLength(2000)]
  },
  {
    validators: [
      (group) => {
        const debut = group.get('dateIntervention')?.value;
        const fin = group.get('dateFinIntervention')?.value;

        if (debut && fin && debut > fin) {
          return { datesInterventionInvalides: true };
        }

        return null;
      }
    ]
  }
);

ngOnInit(): void {
  this.chargerBiens();
  this.chargerTickets();
  this.chargerPrestataires();
}

  ngOnDestroy(): void {
    this.libererApercus(this.apercusPhotos);
    this.libererApercus(this.apercusPhotosSupplementaires);
  }

  get unitesOptions(): Option<string>[] {
    return this.unites.map(unite => ({
      label: [
        unite.reference,
        this.libelleTypeUnite(unite.type),
        unite.superficie != null ? `${unite.superficie} m²` : null
      ].filter(Boolean).join(' - '),
      value: unite.id
    }));
  }
  get locatairesOptions(): LocataireOption[] {
    return this.locataires;
  }

  get nombrePhotosExistantes(): number {
    return this.ticketSelectionne?.photos?.length ?? 0;
  }

  get nombreTotalPhotosFormulaire(): number {
    return (this.modeEdition ? this.photosExistantes.length : 0)
      + this.photos.length;
  }

  // ============================================================
  // Biens immobiliers et unités locatives
  // ============================================================

  chargerBiens(): void {
    this.loadingBiens = true;

    this.biensService.getBiens()
      .pipe(finalize(() => this.loadingBiens = false))
      .subscribe({
        next: (resultat: BienDto[]) => {
          this.biens = resultat ?? [];

          this.biensOptions = this.biens.map(bien => {
            const b = bien as BienDto & {
              id: string;
              reference?: string;
              nom?: string;
              ville?: string;
              quartier?: string;
            };

            return {
              label: [
                b.reference,
                b.nom,
                b.ville,
                b.quartier
              ]
                .filter(valeur =>
                  valeur != null && String(valeur).trim() !== ''
                )
                .join(' — ') || 'Bien immobilier',
              value: b.id
            };
          });
        },
        error: (erreur: HttpErrorResponse) => {
          this.afficherErreur(
            erreur,
            'Impossible de charger les biens immobiliers.'
          );
        }
      });
  }

  champIntervenantInvalide(nom: string): boolean {
  const controle = this.formulaireIntervenant.get(nom);

  return !!controle
    && controle.invalid
    && (controle.touched || controle.dirty);
}

private dateIntervenantPourInput(
  valeur: string | Date | null | undefined
): string {
  if (!valeur) {
    return '';
  }

  if (typeof valeur === 'string') {
    return valeur.length >= 10
      ? valeur.substring(0, 10)
      : valeur;
  }

  return this.dateLocaleISO(valeur);
}

private chargerPrestataires(): void {
  this.loadingPrestataires = true;

  this.prestatairesService
    .getPrestataires(undefined, undefined, true)
    .pipe(
      finalize(() => {
        this.loadingPrestataires = false;
      })
    )
    .subscribe({
      next: (prestataires: PrestataireDto[]) => {
        this.prestatairesOptions = (prestataires ?? []).map(p => {
          const nomPersonne = [
            p.prenom?.trim(),
            p.nom?.trim()
          ]
            .filter(Boolean)
            .join(' ');

          const nom = p.nomEntreprise?.trim()
            || nomPersonne
            || p.nom?.trim()
            || 'Prestataire sans nom';

          return {
            label: `${nom} — ${p.specialite || 'Spécialité non précisée'}`
              + (p.telephone ? ` (${p.telephone})` : ''),
            value: p.id
          };
        });
      },
      error: (erreur: HttpErrorResponse) => {
        this.prestatairesOptions = [];

        this.afficherErreur(
          erreur,
          'Impossible de charger les prestataires.'
        );
      }
    });
}


  /**
   * Appelée lorsque l'utilisateur sélectionne ou change de bien.
   * Réinitialise les sélections dépendantes et charge les unités du bien.
   */

  
  changerBien(bienId: string | null): void {
    const id = bienId ? String(bienId) : '';

    this.formulaire.patchValue({
      bienImmobilierId: id,
      uniteLocativeId: '',
      locataireId: ''
    });

    this.unites = [];
    this.locataires = [];

    // Invalide les éventuelles requêtes de locataires précédentes.
    this.requeteLocatairesUnite++;

    if (!id) {
      this.loadingUnites = false;
      this.loadingLocataires = false;
      this.requeteUnitesBien++;
      return;
    }

    this.chargerUnitesDuBien(id);
  }

  /**
   * Charge les unités d'un bien.
   * uniteIdAConserver est utilisé lors de la modification d'un ticket.
   */
  private chargerUnitesDuBien(
    bienId: string,
    uniteIdAConserver?: string
  ): void {
    const numeroRequete = ++this.requeteUnitesBien;

    this.loadingUnites = true;

    this.unitesService.getUnitesParBien(bienId)
      .pipe(
        finalize(() => {
          if (numeroRequete === this.requeteUnitesBien) {
            this.loadingUnites = false;
          }
        })
      )
      .subscribe({
        next: (resultat: UniteLocativeDto[]) => {
          if (numeroRequete !== this.requeteUnitesBien) {
            return;
          }

          this.unites = resultat ?? [];

          if (uniteIdAConserver) {
            const uniteExiste = this.unites.some(
              unite => unite.id.toLowerCase() ===
                uniteIdAConserver.toLowerCase()
            );

            this.formulaire.patchValue({
              uniteLocativeId: uniteExiste ? uniteIdAConserver : ''
            });
            
          }
        },
        error: (erreur: HttpErrorResponse) => {
          if (numeroRequete !== this.requeteUnitesBien) {
            return;
          }

          this.unites = [];
          this.formulaire.patchValue({ uniteLocativeId: '' });

          this.afficherErreur(
            erreur,
            'Impossible de charger les unités de ce bien.'
          );
        }
      });
  }

  chargerUnites(): void {
    const bienId = String(
      this.formulaire.get('bienImmobilierId')?.value ?? ''
    );

    if (bienId) {
      this.chargerUnitesDuBien(bienId);
    } else {
      this.unites = [];
    }
  }

  get uniteLocativeSelectionnee(): string {
    return String(
      this.formulaire.get('uniteLocativeId')?.value ?? ''
    );
  }

  /**
   * Charge les locataires d'une unité.
   * Le deuxième paramètre permet de conserver le locataire en édition.
   */
  chargerLocatairesParUnite(
    uniteLocativeId: string,
    locataireIdAConserver?: string
  ): void {
    const numeroRequete = ++this.requeteLocatairesUnite;

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
          if (numeroRequete === this.requeteLocatairesUnite) {
            this.loadingLocataires = false;
          }
        })
      )
      .subscribe({
        next: (resultat: any[]) => {
          if (numeroRequete !== this.requeteLocatairesUnite) {
            return;
          }

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
              locataire =>
                locataire.value.toLowerCase() ===
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
          if (numeroRequete !== this.requeteLocatairesUnite) {
            return;
          }

          this.locataires = [];
          this.formulaire.patchValue({ locataireId: '' });

          this.afficherErreur(
            erreur,
            'Impossible de charger les locataires.'
          );
        }
      });
  }

  // ============================================================
  // Liste et filtres
  // ============================================================

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
      ]
        .filter(Boolean)
        .join(' ')
        .toLocaleLowerCase();

      const correspondRecherche =
        !recherche || texte.includes(recherche);

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

  // ============================================================
  // Création et modification d'un ticket
  // ============================================================

ouvrirCreation(): void {
  this.modeEdition = false;
  this.ticketIdEdition = null;

  this.formulaire.reset({
    reference: '',
    bienImmobilierId: '',
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
  this.unites = [];

  this.nettoyerPhotos();
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
    this.photosExistantes = [];
    this.ticketSelectionne = null;
    this.modalDetailVisible = false;
    this.modalCreationVisible = false;
    this.loadingDetail = true;

    this.ticketsService.obtenirParId(t.id)
      .pipe(finalize(() => this.loadingDetail = false))
      .subscribe({
        next: (detail: TicketIncidentDetailDto) => {
          const d = detail as any;

          const photosBrutes = d.photos ?? d.Photos ?? [];
          const photosChargees: TicketPhotoDto[] =
            Array.isArray(photosBrutes) ? photosBrutes : [];

          this.photosExistantes = [...photosChargees];

          this.ticketSelectionne = {
            ...detail,
            photos: photosChargees
          } as TicketIncidentDetailDto;

          const bienId = String(
            d.bienImmobilierId ??
            d.BienImmobilierId ??
            d.bienImmobilier?.id ??
            d.BienImmobilier?.Id ??
            ''
          );

          const uniteId = String(
            d.uniteLocativeId ??
            d.UniteLocativeId ??
            ''
          );

          const locataireId = String(
            d.locataireId ??
            d.LocataireId ??
            ''
          );

          this.formulaire.reset({
            reference: d.reference ?? d.Reference ?? t.reference ?? '',
            bienImmobilierId: bienId,
            uniteLocativeId: uniteId,
            locataireId,
            categorie:
              d.categorie ?? d.Categorie ?? CategorieIncident.Autre,
            description:
              d.description ?? d.Description ?? '',
            priorite:
              d.priorite ?? d.Priorite ?? PrioriteIncident.Moyenne,
            dateincident: this.formaterDatePourInput(
              d.dateincident ?? d.dateIncident ?? d.DateIncident
            ),
            coutEstime: d.coutEstime ?? d.CoutEstime ?? null,
            imputableA:
              d.imputableA ??
              d.ImputableA ??
              ImputabiliteIncident.Indeterminee,
            validationDevisRequise:
              d.validationDevisRequise ??
              d.ValidationDevisRequise ??
              false,
            typePhoto:
              d.typePhoto ??
              d.TypePhoto ??
              TypePhotoIncident.AvantIntervention
          });

          this.unites = [];
          this.locataires = [];

          if (bienId) {
            this.chargerUnitesDuBien(bienId, uniteId);
          }

          if (uniteId) {
            this.chargerLocatairesParUnite(uniteId, locataireId);
          }

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

    const bienImmobilierId = String(
      v.bienImmobilierId ?? ''
    ).trim();

    if (!bienImmobilierId) {
      this.formulaire.get('bienImmobilierId')?.setErrors({
        required: true
      });

      this.messages.add({
        severity: 'warn',
        summary: 'Bien immobilier obligatoire',
        detail: 'Sélectionne le bien immobilier concerné par l’incident.'
      });
      return;
    }

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

    if (
      v.coutEstime !== null &&
      v.coutEstime !== '' &&
      (!Number.isFinite(Number(v.coutEstime)) || Number(v.coutEstime) < 0)
    ) {
      this.messages.add({
        severity: 'error',
        summary: 'Coût invalide',
        detail: 'Le coût estimé doit être un montant positif ou nul.'
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

    // ----------------------------------------------------------
    // Modification d'un ticket existant
    // ----------------------------------------------------------

    if (this.modeEdition && this.ticketIdEdition) {
      const id = this.ticketIdEdition;
      const nouvellesPhotos = [...this.photos];
      const typePhoto = v.typePhoto as TypePhotoIncident;

      const demande: ModifierTicketIncidentRequest = {
        categorie: v.categorie,
        description: String(v.description ?? '').trim(),
        descriptionDegats:
          String(v.descriptionDegats ?? '').trim() || null,
        priorite: v.priorite,
        coutEstime:
          v.coutEstime === null || v.coutEstime === ''
            ? null
            : Number(v.coutEstime),
        imputableA: v.imputableA,
        validationDevisRequise: !!v.validationDevisRequise
      };

      this.ticketsService.modifier(id, demande).subscribe({
        next: () => {
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

          this.ticketsService
            .ajouterPhotos(id, nouvellesPhotos, typePhoto)
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

    // ----------------------------------------------------------
    // Création : BienImmobilierId est obligatoire côté API.
    // ----------------------------------------------------------

    const formData = new FormData();

    formData.append('Reference', reference);
    formData.append('BienImmobilierId', bienImmobilierId);

    if (v.uniteLocativeId) {
      formData.append('UniteLocativeId', String(v.uniteLocativeId));
    }

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

  supprimerPhotoExistante(index: number): void {
    if (index < 0 || index >= this.photosExistantes.length) {
      return;
    }

    this.photosExistantes.splice(index, 1);
  }

  supprimerPhoto(index: number): void {
    const apercu = this.apercusPhotos[index];

    if (apercu?.startsWith('blob:')) {
      URL.revokeObjectURL(apercu);
    }

    this.apercusPhotos.splice(index, 1);
    this.photos.splice(index, 1);
  }

  nettoyerPhotos(): void {
    this.libererApercus(this.apercusPhotos);
    this.photos = [];
    this.apercusPhotos = [];
  }

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

    if (statut === StatutIncident.Resolu) {
      this.ticketEnResolution = ticket;
      this.resultatReparationSaisi =
        String((ticket as any).resultatReparation ?? '');
      this.modalResolutionVisible = true;
      return;
    }

    this.confirmation.confirm({
      header: 'Confirmer le changement',
      message:
        `Veux-tu changer le statut du ticket vers « ${this.libelleStatut(statut)} » ?`,
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

ouvrirAjoutIntervenant(): void {
  if (!this.ticketSelectionne || this.saving) {
    return;
  }

  this.modeEditionIntervenant = false;
  this.intervenantIdEdition = null;

  this.formulaireIntervenant.reset({
    prestataireId: '',

    montantDevis: null,
    referenceDevis: '',

    montantFacture: null,
    referenceFacture: '',

    dateAssignation: this.dateLocaleISO(),
    dateIntervention: '',
    dateFinIntervention: '',

    observations: ''
  });

  this.formulaireIntervenant.markAsPristine();
  this.formulaireIntervenant.markAsUntouched();

  if (!this.prestatairesOptions.length && !this.loadingPrestataires) {
    this.chargerPrestataires();
  }

  this.modalIntervenantVisible = true;
}

  fermerFormulaireIntervenant(): void {
  if (this.saving) {
    return;
  }

  this.modalIntervenantVisible = false;
  this.formulaireIntervenant.markAsPristine();
  this.formulaireIntervenant.markAsUntouched();
}

modifierIntervenant(intervenant: IntervenantTicket): void {
  if (!this.ticketSelectionne || !intervenant || this.saving) {
    return;
  }

  this.modeEditionIntervenant = true;
  this.intervenantIdEdition = intervenant.id;

  this.formulaireIntervenant.reset({
    prestataireId: intervenant.prestataireId ?? '',

    montantDevis: intervenant.montantDevis ?? null,
    referenceDevis: intervenant.referenceDevis ?? '',

    montantFacture: intervenant.montantFacture ?? null,
    referenceFacture: intervenant.referenceFacture ?? '',

    dateAssignation: this.dateIntervenantPourInput(
      intervenant.dateAssignation
    ),

    dateIntervention: this.dateIntervenantPourInput(
      intervenant.dateIntervention
    ),

    dateFinIntervention: this.dateIntervenantPourInput(
      intervenant.dateFinIntervention
    ),

    observations: intervenant.observations ?? ''
  });

  this.formulaireIntervenant.markAsPristine();
  this.formulaireIntervenant.markAsUntouched();

  if (!this.prestatairesOptions.length && !this.loadingPrestataires) {
    this.chargerPrestataires();
  }

  this.modalIntervenantVisible = true;
}



enregistrerIntervenant(): void {
  this.formulaireIntervenant.markAllAsTouched();

  if (
    this.formulaireIntervenant.invalid ||
    !this.ticketSelectionne ||
    this.saving
  ) {
    if (this.formulaireIntervenant.hasError('datesInterventionInvalides')) {
      this.messages.add({
        severity: 'warn',
        summary: 'Dates incohérentes',
        detail:
          'La date de fin ne peut pas précéder la date de début de l’intervention.'
      });
    }

    return;
  }

  const v = this.formulaireIntervenant.getRawValue();

  const prestataireId = String(v.prestataireId ?? '').trim();

  if (!prestataireId) {
    this.messages.add({
      severity: 'warn',
      summary: 'Prestataire obligatoire',
      detail: 'Sélectionne un prestataire dans la liste.'
    });
    return;
  }

  const convertirMontant = (
    valeur: unknown
  ): number | null => {
    if (valeur === null || valeur === undefined || valeur === '') {
      return null;
    }

    const montant = Number(valeur);

    return Number.isFinite(montant) ? montant : NaN;
  };

  const montantDevis = convertirMontant(v.montantDevis);
  const montantFacture = convertirMontant(v.montantFacture);

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

  const demande: ModifierTicketIntervenantRequest = {
    prestataireId,

    montantDevis,
    referenceDevis: String(v.referenceDevis ?? '').trim() || null,

    montantFacture,
    referenceFacture: String(v.referenceFacture ?? '').trim() || null,

    dateAssignation: v.dateAssignation || null,
    dateIntervention: v.dateIntervention || null,
    dateFinIntervention: v.dateFinIntervention || null,

    observations: String(v.observations ?? '').trim() || null
  };

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
    .pipe(finalize(() => {
      this.saving = false;
    }))
    .subscribe({
      next: () => {
        this.messages.add({
          severity: 'success',
          summary: estModification
            ? 'Intervenant modifié'
            : 'Intervenant ajouté',
          detail: estModification
            ? 'Les informations de l’affectation ont été enregistrées.'
            : 'Le prestataire a été affecté au ticket.'
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


supprimerIntervenant(intervenant: IntervenantTicket): void {
  const ticket = this.ticketSelectionne;

  if (!ticket || !intervenant || this.saving) {
    return;
  }

  this.confirmation.confirm({
    header: 'Supprimer l’intervenant',
    message: `Voulez-vous vraiment supprimer l’intervenant « ${intervenant.nomPrestataire} » ?`,
    icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'Supprimer',
    rejectLabel: 'Annuler',
    acceptButtonStyleClass: 'p-button-danger',
    rejectButtonStyleClass: 'p-button-secondary',

    accept: () => {
      this.saving = true;

      this.ticketsService
        .supprimerIntervenant(ticket.id, intervenant.id)
        .pipe(
          finalize(() => {
            this.saving = false;
          })
        )
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
    this.typePhotoSupplementaire = TypePhotoIncident.AvantIntervention;
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

    this.ticketsService.ajouterPhotos(id, fichiers, typePhoto)
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

libelleTypeUnite(type: TypeUniteLocative | null | undefined): string {
  if (type == null) {
    return 'Type non précisé';
  }

  return TypeUniteLocative[type] ?? 'Type inconnu';
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
