import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api'; // 1. Importer le service

import { ParametresService } from '../services/parametres.service';

@Component({
  selector: 'app-parametres',
  standalone: true,
  imports: [CommonModule, FormsModule, InputTextModule, ButtonModule, CardModule, ToastModule],
  providers: [MessageService], 
  templateUrl: './parametres.html',
  styleUrls: ['./parametres.scss']
})
export class Parametres implements OnInit {
  private readonly parametresService = inject(ParametresService);
  private readonly messageService = inject(MessageService); // 3. Injecter le service

  parametres: any = {
    nomSociete: '',
    adresse: '',
    telephone: '',
    email: '',
    piedDePage: ''
  };

  chargementEnCours: boolean = false;

  ngOnInit(): void {
    this.chargerParametres();
  }

  chargerParametres(): void {
    this.chargementEnCours = true;
    this.parametresService.getParametres().subscribe({
      next: (data) => {
        this.parametres = data;
        this.chargementEnCours = false;
      },
      error: (err) => {
        console.error('Erreur chargement paramètres :', err);
        this.chargementEnCours = false;
        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: 'Impossible de charger les paramètres.'
        });
      }
    });
  }

  sauvegarderParametres(): void {
    this.parametresService.sauvegarderParametres(this.parametres).subscribe({
      next: () => {
        // Remplacement de l'alerte par le Toast de succès
        this.messageService.add({
          severity: 'success',
          summary: 'Succès',
          detail: 'Paramètres enregistrés avec succès !'
        });
      },
      error: (err) => {
        console.error('Erreur sauvegarde :', err);
        // Remplacement de l'alerte par le Toast d'erreur
        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: "Erreur lors de l'enregistrement des paramètres."
        });
      }
    });
  }
}