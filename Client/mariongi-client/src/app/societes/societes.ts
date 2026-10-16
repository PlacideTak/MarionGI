
import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  Validators
} from '@angular/forms';

import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { SelectModule } from 'primeng/select';
import { SocietesService } from '../services/societes.service';

import {
  SocieteDto,
  ModifierSocieteRequest,
  TypePersonne,
  FormeJuridique
} from '../models/gestimmo.models';

@Component({
  selector: 'app-societes',
  standalone: true,
  imports: [
    CommonModule,
    SelectModule,
    ReactiveFormsModule,
    ButtonModule,
    CardModule,
    TagModule,
    DialogModule,
    InputTextModule,
    TableModule,
    ConfirmDialogModule
  ],
  providers: [ConfirmationService],
  templateUrl: './societes.html',
  styleUrls: ['./societes.scss']
})
export class Societes implements OnInit {

  private readonly societesService = inject(SocietesService);
  private readonly fb = inject(FormBuilder);
  private readonly confirmationService = inject(ConfirmationService);

  readonly societes = signal<SocieteDto[]>([]);
  readonly isLoading = signal(false);
  readonly displayModal = signal(false);
  readonly isEditMode = signal(false);
  readonly selectedSociete = signal<SocieteDto | null>(null);
  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  readonly TypePersonne = TypePersonne;
  readonly FormeJuridique = FormeJuridique;

  readonly typesPersonne = [
    { label: 'Personne physique', value: TypePersonne.PersonnePhysique },
    { label: 'Personne morale', value: TypePersonne.PersonneMorale }
  ];

  readonly formesJuridiques = [
    { label: 'Entreprise individuelle', value: FormeJuridique.EntrepriseIndividuelle },
    { label: 'SARL', value: FormeJuridique.SARL },
    { label: 'SA', value: FormeJuridique.SA },
    { label: 'SAS', value: FormeJuridique.SAS },
    { label: 'SNC', value: FormeJuridique.SNC },
    { label: 'SCS', value: FormeJuridique.SCS },
    { label: 'GIE', value: FormeJuridique.GIE },
    { label: 'Autre', value: FormeJuridique.Autre }
  ];

  readonly societeForm = this.fb.group({
    typePersonne: [
      TypePersonne.PersonneMorale,
      Validators.required
    ],
    nom: ['', Validators.required],
    prenom: [''],
    formeJuridique: [null as FormeJuridique | null],

    numeroEntreprise: [''],
    numeroContribuable: [''],

    adresse: [''],
    adresseFiscale: [''],
    ville: [''],
    region: [''],
    departement: [''],
    codePostal: [''],
    pays: ['Cameroun'],

    centreImpots: [''],
    activitePrincipale: [''],

    telephone: [''],
    email: ['', Validators.email]
  });

  ngOnInit(): void {
    this.societeForm.controls.typePersonne.valueChanges.subscribe(type => {
      this.appliquerReglesTypePersonne(type);
    });

    this.chargerSocietes();
  }

  get estPersonnePhysique(): boolean {
    return this.societeForm.controls.typePersonne.value
      === TypePersonne.PersonnePhysique;
  }

  get estPersonneMorale(): boolean {
    return this.societeForm.controls.typePersonne.value
      === TypePersonne.PersonneMorale;
  }

  private appliquerReglesTypePersonne(
    type: TypePersonne | null
  ): void {
    const prenom = this.societeForm.controls.prenom;
    const forme = this.societeForm.controls.formeJuridique;

    if (type === TypePersonne.PersonnePhysique) {
      prenom.setValidators([Validators.required]);
      forme.clearValidators();
      forme.setValue(null);
      this.societeForm.controls.numeroEntreprise.setValue('');
    } else {
      prenom.clearValidators();
      prenom.setValue('');
      forme.setValidators([Validators.required]);
    }

    prenom.updateValueAndValidity({ emitEvent: false });
    forme.updateValueAndValidity({ emitEvent: false });
  }

  chargerSocietes(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.societesService.getSocietes().subscribe({
      next: data => {
        this.societes.set(Array.isArray(data) ? data : []);
        this.isLoading.set(false);
      },
      error: err => {
        console.error('Erreur lors du chargement des propriétaires :', err);
        this.errorMessage.set(
          typeof err?.error === 'string'
            ? err.error
            : 'Impossible de charger les propriétaires.'
        );
        this.isLoading.set(false);
      }
    });
  }

  ouvrirModalAjout(): void {
    this.isEditMode.set(false);
    this.selectedSociete.set(null);

    this.societeForm.reset({
      typePersonne: TypePersonne.PersonneMorale,
      nom: '',
      prenom: '',
      formeJuridique: null,
      numeroEntreprise: '',
      numeroContribuable: '',
      adresse: '',
      adresseFiscale: '',
      ville: '',
      region: '',
      departement: '',
      codePostal: '',
      pays: 'Cameroun',
      centreImpots: '',
      activitePrincipale: '',
      telephone: '',
      email: ''
    });

    this.appliquerReglesTypePersonne(TypePersonne.PersonneMorale);
    this.displayModal.set(true);
  }

  ouvrirModalModification(societe: SocieteDto): void {
    this.isEditMode.set(true);
    this.selectedSociete.set(societe);

    this.societeForm.reset({
      typePersonne: societe.typePersonne,
      nom: societe.nom,
      prenom: societe.prenom ?? '',
      formeJuridique:
        societe.formeJuridique as FormeJuridique | null ?? null,
      numeroEntreprise: societe.numeroEntreprise ?? '',
      numeroContribuable: societe.numeroContribuable ?? '',
      adresse: societe.adresse ?? '',
      adresseFiscale: societe.adresseFiscale ?? '',
      ville: societe.ville ?? '',
      region: societe.region ?? '',
      departement: societe.departement ?? '',
      codePostal: societe.codePostal ?? '',
      pays: societe.pays ?? 'Cameroun',
      centreImpots: societe.centreImpots ?? '',
      activitePrincipale: societe.activitePrincipale ?? '',
      telephone: societe.telephone ?? '',
      email: societe.email ?? ''
    });

    this.appliquerReglesTypePersonne(societe.typePersonne);

    // Restaurer la forme juridique après application des règles.
    if (societe.typePersonne === TypePersonne.PersonneMorale) {
      this.societeForm.controls.formeJuridique.setValue(
        societe.formeJuridique as FormeJuridique | null ?? null
      );
    }

    this.displayModal.set(true);
  }

enregistrerSociete(): void {
  if (this.societeForm.invalid) {
    this.societeForm.markAllAsTouched();
    return;
  }

  const values = this.societeForm.getRawValue();
  const typePersonne = values.typePersonne;

  if (typePersonne === null) {
    return;
  }

  const request: ModifierSocieteRequest = {
    typePersonne,

    nom: values.nom?.trim() ?? '',

    prenom:
      typePersonne === TypePersonne.PersonnePhysique
        ? values.prenom?.trim() || null
        : null,

    formeJuridique:
      typePersonne === TypePersonne.PersonneMorale
        ? values.formeJuridique
        : null,

    numeroEntreprise:
      typePersonne === TypePersonne.PersonneMorale
        ? values.numeroEntreprise?.trim() || null
        : null,

    numeroContribuable:
      values.numeroContribuable?.trim() || null,

    adresse: values.adresse?.trim() || null,
    adresseFiscale: values.adresseFiscale?.trim() || null,
    ville: values.ville?.trim() || null,
    region: values.region?.trim() || null,
    departement: values.departement?.trim() || null,
    codePostal: values.codePostal?.trim() || null,
    pays: values.pays?.trim() || null,

    centreImpots: values.centreImpots?.trim() || null,
    activitePrincipale: values.activitePrincipale?.trim() || null,

    telephone: values.telephone?.trim() || null,
    email: values.email?.trim() || null
  };

  const societe = this.selectedSociete();
  const modeModification = this.isEditMode();

  // En mode modification, un propriétaire doit être sélectionné.
  if (modeModification && !societe) {
    this.errorMessage.set(
      'Impossible de modifier le propriétaire : aucun propriétaire sélectionné.'
    );
    return;
  }

  this.errorMessage.set('');
  this.successMessage.set('');
  this.isLoading.set(true);

  const traiterSucces = (): void => {
    this.displayModal.set(false);

    this.successMessage.set(
      modeModification
        ? 'Propriétaire modifié avec succès.'
        : 'Propriétaire créé avec succès.'
    );

    this.chargerSocietes();
  };

  const traiterErreur = (err: any): void => {
    console.error(
      'Erreur lors de l’enregistrement du propriétaire :',
      err
    );

    this.errorMessage.set(
      typeof err?.error === 'string'
        ? err.error
        : err?.error?.message ??
          'Impossible d’enregistrer le propriétaire.'
    );

    this.isLoading.set(false);
  };

  if (modeModification && societe) {
    this.societesService
      .updateSociete(societe.id, request)
      .subscribe({
        next: traiterSucces,
        error: traiterErreur
      });

    return;
  }

  this.societesService
    .createSociete(request)
    .subscribe({
      next: traiterSucces,
      error: traiterErreur
    });
}

  basculerStatut(societe: SocieteDto): void {
    const nouveauStatut = !societe.actif;
    const nom = this.getNomAffiche(societe);

    this.confirmationService.confirm({
      message: `Voulez-vous ${nouveauStatut ? 'activer' : 'désactiver'} le compte de « ${nom} » ?`,
      header: nouveauStatut ? 'Activer le compte' : 'Désactiver le compte',
      icon: nouveauStatut ? 'pi pi-check-circle' : 'pi pi-exclamation-triangle',
      acceptLabel: 'Oui',
      rejectLabel: 'Annuler',
      accept: () => {
        this.isLoading.set(true);

        this.societesService.updateStatut(
          societe.id,
          { actif: nouveauStatut }
        ).subscribe({
          next: () => {
            this.successMessage.set(
              nouveauStatut
                ? 'Compte activé avec succès.'
                : 'Compte désactivé avec succès.'
            );
            this.chargerSocietes();
          },
          error: err => {
            console.error('Erreur lors du changement de statut :', err);
            this.errorMessage.set(
              typeof err?.error === 'string'
                ? err.error
                : 'Impossible de modifier le statut du compte.'
            );
            this.isLoading.set(false);
          }
        });
      }
    });
  }

  supprimerSociete(societe: SocieteDto): void {
    const nom = this.getNomAffiche(societe);

    this.confirmationService.confirm({
      message: `Voulez-vous vraiment supprimer le compte de « ${nom} » ?`,
      header: 'Confirmation de suppression',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Supprimer',
      rejectLabel: 'Annuler',
      accept: () => {
        this.isLoading.set(true);

        this.societesService.deleteSociete(societe.id).subscribe({
          next: () => {
            this.successMessage.set('Compte supprimé avec succès.');
            this.chargerSocietes();
          },
          error: err => {
            console.error('Erreur lors de la suppression :', err);
            this.errorMessage.set(
              typeof err?.error === 'string'
                ? err.error
                : 'Impossible de supprimer le compte.'
            );
            this.isLoading.set(false);
          }
        });
      }
    });
  }

  getNomAffiche(societe: SocieteDto): string {
    if (societe.typePersonne === TypePersonne.PersonnePhysique) {
      return `${societe.prenom ?? ''} ${societe.nom}`.trim();
    }

    return societe.nom;
  }

  getTypePersonneLabel(type: TypePersonne): string {
    return type === TypePersonne.PersonnePhysique
      ? 'Personne physique'
      : 'Personne morale';
  }

  getFormeJuridiqueLabel(forme: number | null | undefined): string {
    return this.formesJuridiques.find(f => f.value === forme)?.label
      ?? (forme === FormeJuridique.NonApplicable ? 'Non applicable' : 'Non renseignée');
  }

  fermerModal(): void {
    this.displayModal.set(false);
    this.societeForm.reset();
  }
}
