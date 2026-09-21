import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { AuthService } from '../login/auth.service';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { DialogModule } from 'primeng/dialog';

@Component({
  selector: 'app-mot-de-passe-oublie',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, InputTextModule, PasswordModule, DialogModule],
  templateUrl: './mot-de-passe-oublie.html'
})
export class MotDePasseOublie {
  private readonly authService = inject(AuthService);
  private readonly messageService = inject(MessageService);

  displayModal: boolean = false;
  etape: number = 1;

  identifiant: string = '';
  codeOtp: string = '';
  nouveauMotDePasse: string = '';
  chargement: boolean = false;
  
  // Variable dédiée pour afficher l'erreur proprement dans la modale
  errorMessage: string | null = null;

  ouvrirModal(): void {
    this.etape = 1;
    this.identifiant = '';
    this.codeOtp = '';
    this.nouveauMotDePasse = '';
    this.errorMessage = null;
    this.displayModal = true;
  }

  envoyerDemande(): void {
    this.errorMessage = null;
    if (!this.identifiant.trim()) {
      this.errorMessage = 'Veuillez entrer votre téléphone ou e-mail.';
      return;
    }

    this.chargement = true;
    this.authService.demanderRecuperation(this.identifiant).subscribe({
      next: () => {
        this.chargement = false;
        this.errorMessage = null;
        this.messageService.add({ severity: 'success', summary: 'Code envoyé', detail: 'Vérifiez votre téléphone ou e-mail.' });
        this.etape = 2;
      },
      error: (err) => {
        this.chargement = false;
 
        // Analyse élargie de l'objet d'erreur retourné par le backend
        const errorBody = err.error;

        if (typeof errorBody === 'string') {
          this.errorMessage = errorBody;
        } else if (errorBody?.message) {
          this.errorMessage = errorBody.message;
        } else if (errorBody?.title) {
          this.errorMessage = errorBody.title;
        } else if (errorBody?.errors) {
          // Si c'est une erreur de validation ModelState d'ASP.NET Core
          const firstKey = Object.keys(errorBody.errors)[0];
          if (firstKey && errorBody.errors[firstKey].length > 0) {
            this.errorMessage = errorBody.errors[firstKey][0];
          }
        } else {
          // Si rien d'autre ne matche, on affiche le message textuel d'Angular ou un repli
          this.errorMessage = err.message || 'Impossible d\'initier la récupération.';
        }
      }
    });
  }

  validerReinitialisation(): void {
    this.errorMessage = null;
    if (!this.codeOtp || !this.nouveauMotDePasse) {
      this.errorMessage = 'Veuillez remplir tous les champs.';
      return;
    }

    this.chargement = true;
    const payload = {
      identifiant: this.identifiant,
      codeOtp: this.codeOtp,
      nouveauMotDePasse: this.nouveauMotDePasse
    };

    this.authService.reinitialiserMotDePasse(payload).subscribe({
      next: () => {
        this.chargement = false;
        this.errorMessage = null;
        this.messageService.add({ severity: 'success', summary: 'Succès', detail: 'Mot de passe réinitialisé.' });
        this.displayModal = false;
      },
      error: (err) => {
        this.chargement = false;
        if (typeof err.error === 'string') {
          this.errorMessage = err.error;
        } else if (err.error?.message) {
          this.errorMessage = err.error.message;
        } else if (err.error?.title) {
          this.errorMessage = err.error.title;
        } else {
          this.errorMessage = 'Échec de la réinitialisation.';
        }
      }
    });
  }
}