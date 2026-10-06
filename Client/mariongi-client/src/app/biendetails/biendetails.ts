import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';

import { BiensService, BienDetailDto } from '../services/biens.service';
import { ROLES } from '../models/gestimmo.models';
import { AuthService } from '../login/auth.service';

@Component({
  selector: 'app-bien-details',
  standalone: true,
  imports: [
    CommonModule,
    ButtonModule
  ],
  templateUrl: './biendetails.html',
  styleUrls: ['./biendetails.scss']
})
export class BienDetails implements OnInit {

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  public readonly bienService = inject(BiensService);
  private readonly authService = inject(AuthService);

  // ============================================================
  // ÉTAT
  // ============================================================

  isLocataire = false;

  bien: BienDetailDto | null = null;

  bienId: string | null = null;

  chargementEnCours = true;

  photoActiveIndex = 0;


  // ============================================================
  // INITIALISATION
  // ============================================================

  ngOnInit(): void {

    this.isLocataire =
      this.authService.hasRole([ROLES.Locataire]);

    this.route.paramMap.subscribe(params => {

      const id = params.get('id');

      this.bienId = id;

      if (!id) {
        this.chargementEnCours = false;
        return;
      }

      this.chargerBien(id);
    });
  }


  // ============================================================
  // CHARGEMENT DU BIEN
  // ============================================================

  private chargerBien(id: string): void {

    this.chargementEnCours = true;

    this.bienService.getBienById(id).subscribe({

      next: (data) => {

        this.bien = data;
        this.photoActiveIndex = 0;
        this.chargementEnCours = false;

      },

      error: (err) => {

        console.error(
          'Erreur lors de la récupération du bien :',
          err
        );

        this.bien = null;
        this.chargementEnCours = false;

      }

    });
  }


  // ============================================================
  // NAVIGATION
  // ============================================================

  allerVersListe(): void {

    this.router.navigate(['/biens']);

  }


  // ============================================================
  // MODIFICATION DU BIEN
  // ============================================================

modifierBien(): void {
  if (this.isLocataire || !this.bien) {
    return;
  }

  this.router.navigate(['/biens'], {
    queryParams: {
      edit: this.bien.id
    }
  });
}


  // ============================================================
  // GESTION DES UNITÉS LOCATIVES
  // ============================================================

gererUnites(): void {
  if (this.isLocataire || !this.bien) {
    return;
  }

  this.router.navigate(['/uniteslocatives'], {
    queryParams: {
      bienId: this.bien.id
    }
  });
}

}