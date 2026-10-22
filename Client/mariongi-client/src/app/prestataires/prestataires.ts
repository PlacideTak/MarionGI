
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
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TextareaModule } from 'primeng/textarea';

import {
  MessageService,
  ConfirmationService
} from 'primeng/api';

import { PrestatairesService } from '../services/prestataire.service';
import { AuthService } from '../login/auth.service';

import {
  type PrestataireDto,
  type CreerPrestataireRequest,
  type ModifierPrestataireRequest,
  TypePersonne
} from '../models/gestimmo.models';

interface DropdownItem {
  label: string;
  value: number;
}

interface StatutOption {
  label: string;
  value: boolean | null;
}

@Component({
  selector: 'app-prestataires',
  standalone: true,

  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,

    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    SelectModule,
    TagModule,
    ToastModule,
    ConfirmDialogModule,
    TextareaModule
  ],

  providers: [
    MessageService,
    ConfirmationService
  ],

  templateUrl: './prestataires.html',
  styleUrl: './prestataires.scss'
})
export class Prestataires implements OnInit {

  // ============================================================
  // DÉPENDANCES
  // ============================================================

  private readonly fb = inject(FormBuilder);

  private readonly prestatairesService =
    inject(PrestatairesService);

  private readonly messageService =
    inject(MessageService);

  private readonly confirmationService =
    inject(ConfirmationService);

  private readonly authService =
    inject(AuthService);

  // ============================================================
  // DONNÉES
  // ============================================================

  prestataires: PrestataireDto[] = [];
  readonly TypePersonne = TypePersonne;
  loading = false;
  saving = false;

  recherche = '';
  filtreType: number | null = null;
  filtreActif: boolean | null = true;

  // ============================================================
  // TYPES
  // IMPORTANT : adapter les valeurs à l'énumération C# réelle.
  // Hypothèse : 0 = personne physique, 1 = entreprise.
  // ============================================================

  statutOptions: StatutOption[] = [
    { label: 'Tous les statuts', value: null },
    { label: 'Actifs', value: true },
    { label: 'Inactifs', value: false }
  ];

  // ============================================================
  // PERMISSIONS
  // ============================================================

  canAccessPrestataires = false;
  canManagePrestataires = false;

  // ============================================================
  // DIALOGUE ET FORMULAIRE
  // ============================================================

  prestataireDialog = false;
  prestataireForm!: FormGroup;

  isEditMode = false;
  selectedPrestataireId?: string;

  // ============================================================
  // INITIALISATION
  // ============================================================

  ngOnInit(): void {
    this.verifierPermissions();
    this.initForm();

    if (this.canAccessPrestataires) {
      this.chargerPrestataires();
    }
  }

  // ============================================================
  // PERMISSIONS
  // ============================================================

  private verifierPermissions(): void {
    this.canAccessPrestataires =
      this.prestatairesService.canAccessPrestataires();

    this.canManagePrestataires =
      this.prestatairesService.canManagePrestataires();
  }

  // ============================================================
  // FORMULAIRE
  // ============================================================

  private initForm(): void {
    this.prestataireForm = this.fb.group({
      typePrestataire: [
        0,
        Validators.required
      ],

      nom: [
        '',
        [
          Validators.required,
          Validators.maxLength(150)
        ]
      ],

      prenom: [
        '',
        Validators.maxLength(150)
      ],

      nomEntreprise: [
        '',
        Validators.maxLength(200)
      ],

      specialite: [
        '',
        [
          Validators.required,
          Validators.maxLength(150)
        ]
      ],

      telephone: [
        '',
        [
          Validators.required,
          Validators.maxLength(30)
        ]
      ],

      courriel: [
        '',
        [
          Validators.email,
          Validators.maxLength(254)
        ]
      ],

      adresse: [
        '',
        Validators.maxLength(250)
      ],

      ville: [
        '',
        Validators.maxLength(100)
      ],

      numeroContribuable: [
        '',
        Validators.maxLength(100)
      ],

      observations: [
        '',
        Validators.maxLength(1000)
      ]
    });
  }



onTypePrestataireChange(): void {
  const type = Number(
    this.prestataireForm.get('typePrestataire')?.value
  );

  const estPersonnePhysique =
    type === TypePersonne.PersonnePhysique;

  const nom = this.prestataireForm.get('nom');
  const prenom = this.prestataireForm.get('prenom');
  const nomEntreprise = this.prestataireForm.get('nomEntreprise');

  if (estPersonnePhysique) {
    // Personne physique : nom et prénom obligatoires.
    nom?.setValidators([
      Validators.required,
      Validators.maxLength(150)
    ]);

    prenom?.setValidators([
      Validators.required,
      Validators.maxLength(150)
    ]);

    // Le nom d'entreprise est facultatif.
    nomEntreprise?.setValidators([
      Validators.maxLength(200)
    ]);
  } else {
    // Personne morale : seul le nom de l'entreprise est obligatoire.
    nom?.clearValidators();
    prenom?.clearValidators();

    nomEntreprise?.setValidators([
      Validators.required,
      Validators.maxLength(200)
    ]);

    // Évite de conserver les valeurs d'une personne physique.
    nom?.setValue('');
    prenom?.setValue('');
  }

  nom?.updateValueAndValidity();
  prenom?.updateValueAndValidity();
  nomEntreprise?.updateValueAndValidity();
}

  // ============================================================
  // CHARGEMENT
  // ============================================================

  chargerPrestataires(): void {
    if (!this.canAccessPrestataires) {
      return;
    }

    this.loading = true;

    this.prestatairesService
      .getPrestataires()
      .subscribe({
        next: (data: PrestataireDto[]) => {
          this.prestataires =
            Array.isArray(data) ? [...data] : [];

          this.loading = false;
        },

        error: (err: HttpErrorResponse) => {
          this.loading = false;
          this.traiterErreurHttp(err, 'chargement');
        }
      });
  }

  // ============================================================
  // RECHERCHE ET FILTRES
  // ============================================================

  get prestatairesFiltres(): PrestataireDto[] {
    const terme = this.recherche.trim().toLowerCase();

    return this.prestataires.filter(prestataire => {
      const nomComplet = this.getNomAffiche(prestataire)
        .toLowerCase();

      const correspondRecherche =
        !terme ||
        nomComplet.includes(terme) ||
        prestataire.specialite?.toLowerCase().includes(terme) ||
        prestataire.telephone?.toLowerCase().includes(terme) ||
        prestataire.courriel?.toLowerCase().includes(terme) ||
        prestataire.ville?.toLowerCase().includes(terme) ||
        prestataire.numeroContribuable?.toLowerCase().includes(terme);

      const correspondType =
        this.filtreType === null ||
        prestataire.typePrestataire === this.filtreType;

      const correspondStatut =
        this.filtreActif === null ||
        prestataire.actif === this.filtreActif;

      return correspondRecherche &&
        correspondType &&
        correspondStatut;
    });
  }

  reinitialiserFiltres(): void {
    this.recherche = '';
    this.filtreType = null;
    this.filtreActif = null;
  }

  // ============================================================
  // INDICATEURS
  // ============================================================

  get nombrePrestatairesActifs(): number {
    return this.prestataires.filter(p => p.actif).length;
  }

  get nombreEntreprises(): number {
    return this.prestataires.filter(
      p => p.typePrestataire === 1
    ).length;
  }

  // ============================================================
  // AFFICHAGE
  // ============================================================

  getNomAffiche(prestataire: PrestataireDto): string {
    if (prestataire.typePrestataire === 1) {
      return prestataire.nomEntreprise?.trim() ||
        [
          prestataire.prenom,
          prestataire.nom
        ].filter(Boolean).join(' ') ||
        'Entreprise sans nom';
    }

    return [
      prestataire.prenom,
      prestataire.nom
    ]
      .filter(value => value?.trim())
      .join(' ') || 'Nom non renseigné';
  }

  getTypeLabel(type: number): string {
    return this.typesPrestataire.find(
      option => option.value === type
    )?.label ?? 'Type inconnu';
  }

  getTypeSeverity(
    type: number
  ): 'info' | 'success' | 'secondary' {
    switch (type) {
      case 0:
        return 'info';
      case 1:
        return 'success';
      default:
        return 'secondary';
    }
  }

  getStatutSeverity(
    actif: boolean
  ): 'success' | 'secondary' {
    return actif ? 'success' : 'secondary';
  }

  // ============================================================
  // CRÉATION
  // ============================================================

  openNew(): void {
    if (!this.canManagePrestataires) {
      this.afficherAccesRefuse();
      return;
    }

    this.isEditMode = false;
    this.selectedPrestataireId = undefined;

    this.prestataireForm.reset({
      typePrestataire: TypePersonne.PersonneMorale,
      nom: '',
      prenom: '',
      nomEntreprise: '',
      specialite: '',
      telephone: '',
      courriel: '',
      adresse: '',
      ville: '',
      numeroContribuable: '',
      observations: ''
    });

    this.prestataireDialog = true;
    this.onTypePrestataireChange();
  }

  // ============================================================
  // MODIFICATION
  // ============================================================

  editPrestataire(prestataire: PrestataireDto): void {
    if (!this.canManagePrestataires) {
      this.afficherAccesRefuse();
      return;
    }

    if (!prestataire.id) {
      this.messageService.add({
        severity: 'error',
        summary: 'Erreur',
        detail: 'L’identifiant du prestataire est introuvable.'
      });

      return;
    }

    this.isEditMode = true;
    this.selectedPrestataireId = prestataire.id;

    this.prestataireForm.reset({
      typePrestataire: prestataire.typePrestataire,
      nom: prestataire.nom ?? '',
      prenom: prestataire.prenom ?? '',
      nomEntreprise: prestataire.nomEntreprise ?? '',
      specialite: prestataire.specialite ?? '',
      telephone: prestataire.telephone ?? '',
      courriel: prestataire.courriel ?? '',
      adresse: prestataire.adresse ?? '',
      ville: prestataire.ville ?? '',
      numeroContribuable: prestataire.numeroContribuable ?? '',
      observations: prestataire.observations ?? ''
    });

    this.prestataireDialog = true;
    this.onTypePrestataireChange();
  }

  // ============================================================
  // SAUVEGARDE
  // ============================================================

  savePrestataire(): void {
    if (!this.canManagePrestataires) {
      this.afficherAccesRefuse();
      return;
    }

    if (this.saving) {
      return;
    }

    if (this.prestataireForm.invalid) {
      this.prestataireForm.markAllAsTouched();
      return;
    }

    const value = this.prestataireForm.getRawValue();

    const typePrestataire = Number(value.typePrestataire);
    const nom = String(value.nom ?? '').trim();
    const prenom = String(value.prenom ?? '').trim();
    const nomEntreprise = String(value.nomEntreprise ?? '').trim();
    const specialite = String(value.specialite ?? '').trim();
    const telephone = String(value.telephone ?? '').trim();
    const courriel = String(value.courriel ?? '').trim();
    const adresse = String(value.adresse ?? '').trim();
    const ville = String(value.ville ?? '').trim();
    const numeroContribuable =
      String(value.numeroContribuable ?? '').trim();
    const observations = String(value.observations ?? '').trim();


    const estPersonnePhysique =
      typePrestataire === TypePersonne.PersonnePhysique;

    const estPersonneMorale =
      typePrestataire === TypePersonne.PersonneMorale;

    if (
      (estPersonnePhysique && (!nom || !prenom)) ||
      (estPersonneMorale && !nomEntreprise) ||
      !specialite ||
      !telephone
    ) {
      this.prestataireForm.markAllAsTouched();
      return;
    }

    const payload: CreerPrestataireRequest = {
      typePrestataire,
      nom,
      prenom,
      nomEntreprise: nomEntreprise || null,
      specialite,
      telephone,
      courriel: courriel || null,
      adresse: adresse || null,
      ville: ville || null,
      numeroContribuable: numeroContribuable || null,
      observations: observations || null
    };

    this.saving = true;

    if (this.isEditMode) {
      if (!this.selectedPrestataireId) {
        this.saving = false;

        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: 'L’identifiant du prestataire est manquant.'
        });

        return;
      }

      const request: ModifierPrestataireRequest = payload;

      this.prestatairesService
        .updatePrestataire(this.selectedPrestataireId, request)
        .subscribe({
          next: () => {
            this.saving = false;

            this.messageService.add({
              severity: 'success',
              summary: 'Succès',
              detail: 'Prestataire modifié avec succès.'
            });

            this.fermerDialog();
            this.chargerPrestataires();
          },

          error: (err: HttpErrorResponse) => {
            this.saving = false;
            this.traiterErreurHttp(err, 'modification');
          }
        });

      return;
    }

    this.prestatairesService
      .createPrestataire(payload)
      .subscribe({
        next: () => {
          this.saving = false;

          this.messageService.add({
            severity: 'success',
            summary: 'Succès',
            detail: 'Prestataire créé avec succès.'
          });

          this.fermerDialog();
          this.chargerPrestataires();
        },

        error: (err: HttpErrorResponse) => {
          this.saving = false;
          this.traiterErreurHttp(err, 'création');
        }
      });
  }

  // ============================================================
  // FERMETURE DU DIALOGUE
  // ============================================================

  fermerDialog(): void {
    if (this.saving) {
      return;
    }

    this.prestataireDialog = false;
    this.isEditMode = false;
    this.selectedPrestataireId = undefined;

    this.prestataireForm.reset({
      typePrestataire: 0,
      nom: '',
      prenom: '',
      nomEntreprise: '',
      specialite: '',
      telephone: '',
      courriel: '',
      adresse: '',
      ville: '',
      numeroContribuable: '',
      observations: ''
    });
  }

  // ============================================================
  // ACTIVATION / DÉSACTIVATION
  // ============================================================

  changerStatut(prestataire: PrestataireDto): void {
    if (!this.canManagePrestataires) {
      this.afficherAccesRefuse();
      return;
    }

    if (!prestataire.id || this.saving) {
      return;
    }

    const nouvelEtat = !prestataire.actif;
    const action = nouvelEtat ? 'activer' : 'désactiver';

    this.confirmationService.confirm({
      header: 'Confirmation',
      message:
        `Voulez-vous vraiment ${action} « ${this.getNomAffiche(prestataire)} » ?`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Confirmer',
      rejectLabel: 'Annuler',

      accept: () => {
        this.prestatairesService
          .modifierStatut(prestataire.id, nouvelEtat)
          .subscribe({
            next: () => {
              this.messageService.add({
                severity: 'success',
                summary: 'Succès',
                detail: `Prestataire ${nouvelEtat ? 'activé' : 'désactivé'} avec succès.`
              });

              this.chargerPrestataires();
            },

            error: (err: HttpErrorResponse) => {
              this.traiterErreurHttp(err, 'modification');
            }
          });
      }
    });
  }

  // ============================================================
  // SUPPRESSION LOGIQUE
  // ============================================================

  deletePrestataire(prestataire: PrestataireDto): void {
    if (!this.canManagePrestataires) {
      this.afficherAccesRefuse();
      return;
    }

    if (!prestataire.id) {
      return;
    }

    this.confirmationService.confirm({
      message:
        `Voulez-vous vraiment supprimer « ${this.getNomAffiche(prestataire)} » ?`,
      header: 'Confirmation de suppression',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Oui, supprimer',
      rejectLabel: 'Annuler',
      acceptButtonStyleClass: 'p-button-danger',

      accept: () => {
        this.prestatairesService
          .deletePrestataire(prestataire.id!)
          .subscribe({
            next: () => {
              this.messageService.add({
                severity: 'success',
                summary: 'Succès',
                detail: 'Prestataire supprimé avec succès.'
              });

              this.chargerPrestataires();
            },

            error: (err: HttpErrorResponse) => {
              this.traiterErreurHttp(err, 'suppression');
            }
          });
      }
    });
  }

  // ============================================================
  // UTILITAIRES
  // ============================================================

  getShortId(id?: string): string {
    return id ? id.substring(0, 8).toUpperCase() : 'N/A';
  }

  private afficherAccesRefuse(): void {
    this.messageService.add({
      severity: 'warn',
      summary: 'Accès refusé',
      detail:
        'Vous ne disposez pas des droits nécessaires pour effectuer cette opération.'
    });
  }


  readonly typesPrestataire = [
    { label: 'Personne physique', value: TypePersonne.PersonnePhysique },
    { label: 'Personne morale', value: TypePersonne.PersonneMorale }
  ];

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
      `Erreur lors du ${action} du prestataire :`,
      err
    );

    if (err.status === 401) {
      this.messageService.add({
        severity: 'error',
        summary: 'Non authentifié',
        detail: 'Votre session a expiré. Veuillez vous reconnecter.'
      });

      return;
    }

    if (err.status === 403) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Accès refusé',
        detail: 'Vous ne disposez pas des droits nécessaires.'
      });

      return;
    }

    const apiMessage =
      err.error?.message ??
      err.error?.title;

    if (typeof apiMessage === 'string' && apiMessage.trim()) {
      this.messageService.add({
        severity: 'error',
        summary: 'Erreur',
        detail: apiMessage,
        life: 5000
      });

      return;
    }

    let detail =
      `Une erreur est survenue lors du ${action} du prestataire (code ${err.status || 'inconnu'}).`;

    if (err.status === 0) {
      detail = 'Impossible de joindre le serveur. Vérifiez votre connexion.';
    } else if (err.status === 409) {
      detail =
        'Cette opération est impossible car le prestataire est peut-être associé à un ticket.';
    }

    this.messageService.add({
      severity: 'error',
      summary: 'Erreur',
      detail,
      life: 5000
    });
  }
}
