import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select'; // ou Select
import { CheckboxModule } from 'primeng/checkbox';
import { UtilisateursService } from '../services/utilisateurs.service';
import { UtilisateurDto } from '../models/gestimmo.models';

@Component({
  selector: 'app-utilisateurs',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    TableModule, ButtonModule, CardModule, TagModule,
    DialogModule, InputTextModule, SelectModule, CheckboxModule
  ],
  templateUrl: './utilisateurs.html',
  styleUrls: ['./utilisateurs.scss']
})
export class Utilisateurs implements OnInit {
  private readonly utilisateursService = inject(UtilisateursService);
  private readonly fb = inject(FormBuilder);

  readonly utilisateurs = signal<UtilisateurDto[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly displayModal = signal<boolean>(false);
  readonly isEditMode = signal<boolean>(false);
  selectedUserId = signal<string | null>(null);

  // Options pour les rôles basées sur l'enum C#
  readonly rolesList = [
    { label: 'Administrateur', value: 1 },
    { label: 'Gestionnaire', value: 2 },
    { label: 'Agent commercial', value: 3 },
    { label: 'Locataire', value: 4 }
  ];

  // Données de la matrice des permissions (statique pour affichage)
  readonly matrixData = [
    { module: 'Gestion des biens', admin: true, gestionnaire: true,  agent: true, locataire: true },
    { module: 'Gestion des contrats', admin: true, gestionnaire: true, agent: false, locataire: true },
    { module: 'Paiements & finances', admin: true, gestionnaire: true,  agent: false, locataire: true },
    { module: 'Gestion des demandes de visite', admin: true, gestionnaire: true,  agent: true, locataire: false },
    { module: 'Rapports', admin: true, gestionnaire: true,  agent: false, locataire: false },
    { module: 'Gestion des utilisateurs', admin: true, gestionnaire: false,  agent: false, locataire: false }
  ];

  userForm = this.fb.group({
    nom: ['', Validators.required],
    prenom: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    telephone: ['', Validators.required],
    role: [2, Validators.required],
    motDePasseProvisoire: [''],
    statut: [true]
  });

  ngOnInit(): void {
    this.chargerUtilisateurs();
  }

  chargerUtilisateurs(): void {
    this.isLoading.set(true);
    this.utilisateursService.getUtilisateurs().subscribe({
      next: (data) => {
        this.utilisateurs.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  ouvrirModalAjout(): void {
    this.isEditMode.set(false);
    this.selectedUserId.set(null);
    this.userForm.reset({ role: 2, statut: true });
    this.userForm.get('motDePasseProvisoire')?.setValidators([Validators.required]);
    this.userForm.get('motDePasseProvisoire')?.updateValueAndValidity();
    this.displayModal.set(true);
  }

  ouvrirModalModification(user: UtilisateurDto): void {
    this.isEditMode.set(true);
    this.selectedUserId.set(user.id);
    this.userForm.patchValue({
      nom: user.nom,
      prenom: user.prenom,
      email: user.email,
      telephone: user.telephone,
      role: user.role,
      statut: user.statut
    });
    this.userForm.get('motDePasseProvisoire')?.clearValidators();
    this.userForm.get('motDePasseProvisoire')?.updateValueAndValidity();
    this.displayModal.set(true);
  }

enregistrerUtilisateur(): void {
  if (this.userForm.invalid) return;

  const formValues = this.userForm.getRawValue();

  if (this.isEditMode() && this.selectedUserId()) {
    const updatePayload = {
      id: this.selectedUserId()!,
      nom: formValues.nom ?? '',
      prenom: formValues.prenom ?? '',
      email: formValues.email ?? '',
      telephone: formValues.telephone ?? '',
      role: Number(formValues.role),
      statut: formValues.statut ?? true
    };

    this.utilisateursService.updateUtilisateur(this.selectedUserId()!, updatePayload).subscribe({
      next: () => {
        this.displayModal.set(false);
        this.chargerUtilisateurs();
      }
    });
  } else {
    const createPayload = {
      nom: formValues.nom ?? '',
      prenom: formValues.prenom ?? '',
      email: formValues.email ?? '',
      telephone: formValues.telephone ?? '',
      role: Number(formValues.role),
      statut: formValues.statut ?? true
    };

    this.utilisateursService.createUtilisateur(createPayload, formValues.motDePasseProvisoire ?? '').subscribe({
      next: () => {
        this.displayModal.set(false);
        this.chargerUtilisateurs();
      }
    });
  }
}
  basculerStatut(user: UtilisateurDto): void {
    const payload = { 
      id: user.id,
      nom: user.nom,
      prenom: user.prenom,
      email: user.email,
      telephone: user.telephone,
      role: user.role,
      statut: !user.statut 
    };

    this.utilisateursService.updateUtilisateur(user.id, payload).subscribe({
      next: () => this.chargerUtilisateurs()
    });
  }

  getRoleLabel(roleId: number): string {
    const role = this.rolesList.find(r => r.value === roleId);
    return role ? role.label : 'Inconnu';
  }
}