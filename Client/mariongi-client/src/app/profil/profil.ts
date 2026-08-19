import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { ProfilService } from '../services/profil.service';

@Component({
  selector: 'app-profil',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule, 
    InputTextModule, 
    PasswordModule,
    ButtonModule, 
    ToastModule
  ],
  providers: [MessageService],
  templateUrl: './profil.html',
  styleUrls: ['./profil.scss']
})
export class Profil implements OnInit {
  profilForm!: FormGroup;
  chargement = false;

  constructor(
    private fb: FormBuilder,
    private messageService: MessageService,
    private profilService: ProfilService
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.chargerProfil();
  }

  private initForm(): void {
    this.profilForm = this.fb.group({
      nom: ['', [Validators.required]],
      prenom: ['', [Validators.required]],
      ancienMotDePasse: [''],
      nouveauMotDePasse: [''],
      confirmationMotDePasse: ['']
    });
  }

  private chargerProfil(): void {
    this.profilService.getProfil().subscribe({
      next: (data) => {
        this.profilForm.patchValue({
          nom: data.nom,
          prenom: data.prenom
        });
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: 'Impossible de charger vos informations de profil.'
        });
      }
    });
  }

  onSubmit(): void {
    if (this.profilForm.invalid) {
      this.profilForm.markAllAsTouched();
      return;
    }

    const valeurs = this.profilForm.value;

    // Validation cohérente des mots de passe si l'un d'eux est rempli
    if (valeurs.nouveauMotDePasse || valeurs.ancienMotDePasse) {
      if (!valeurs.ancienMotDePasse || !valeurs.nouveauMotDePasse) {
        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: 'Veuillez renseigner votre ancien et votre nouveau mot de passe.'
        });
        return;
      }
      if (valeurs.nouveauMotDePasse !== valeurs.confirmationMotDePasse) {
        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: 'Les nouveaux mots de passe ne correspondent pas.'
        });
        return;
      }
    }

    this.chargement = true;

    const payload = {
      nom: valeurs.nom,
      prenom: valeurs.prenom,
      ancienMotDePasse: valeurs.ancienMotDePasse || null,
      nouveauMotDePasse: valeurs.nouveauMotDePasse || null
    };

    this.profilService.modifierProfil(payload).subscribe({
      next: () => {
        this.chargement = false;
        this.messageService.add({
          severity: 'success',
          summary: 'Succès',
          detail: 'Votre profil a été mis à jour avec succès.'
        });
        // Nettoyer les champs de mot de passe
        this.profilForm.patchValue({
          ancienMotDePasse: '',
          nouveauMotDePasse: '',
          confirmationMotDePasse: ''
        });
      },
      error: (err) => {
        this.chargement = false;
        const msg = err.error?.message || 'Une erreur est survenue lors de la mise à jour.';
        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: msg
        });
      }
    });
  }
}