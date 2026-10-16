import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';

import { BiensService, BienDetailDto } from '../services/biens.service';
import { ROLES, StatutFoncier } from '../models/gestimmo.models';
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

  bien: BienDetailDto | null = null;
  bienId: string | null = null;

  chargementEnCours = true;
  erreurChargement = false;
  messageErreur = '';

  photoActiveIndex = 0;

  private requeteEnCours = 0;

  get isLocataire(): boolean {
    return this.authService.hasRole([ROLES.Locataire]);
  }

  get peutGererBien(): boolean {
    return this.authService.hasRole([
      ROLES.Administrateur,
      ROLES.Gestionnaire
    ]);
  }

  get photos(): string[] {
    return this.bien?.photos ?? [];
  }

  get unitesLocatives() {
    return this.bien?.unitesLocatives ?? [];
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');

      this.bienId = id;

      if (!id) {
        this.bien = null;
        this.chargementEnCours = false;
        this.erreurChargement = true;
        this.messageErreur = 'Identifiant du bien manquant.';
        return;
      }

      this.chargerBien(id);
    });
  }

  private chargerBien(id: string): void {
    const requete = ++this.requeteEnCours;

    this.chargementEnCours = true;
    this.erreurChargement = false;
    this.messageErreur = '';
    this.bien = null;
    this.photoActiveIndex = 0;

    this.bienService.getBienById(id).subscribe({
      next: data => {
        if (requete !== this.requeteEnCours) {
          return;
        }

        if (!data) {
          this.bien = null;
          this.erreurChargement = true;
          this.messageErreur = 'Aucun bien immobilier ne correspond à cet identifiant.';
          this.chargementEnCours = false;
          return;
        }

        this.bien = {
          ...data,
          photos: data.photos ?? [],
          unitesLocatives: data.unitesLocatives ?? []
        };

        this.chargementEnCours = false;
      },

      error: err => {
        if (requete !== this.requeteEnCours) {
          return;
        }

        console.error(
          'Erreur lors de la récupération du bien :',
          err
        );

        this.bien = null;
        this.erreurChargement = true;
        this.messageErreur =
          err?.status === 404
            ? 'Ce bien immobilier est introuvable ou n’est plus accessible.'
            : 'Impossible de charger les informations du bien. Vérifie ta connexion et réessaie.';

        this.chargementEnCours = false;
      }
    });
  }

  getStatutFoncierLibelle(
    statut: StatutFoncier | number | null | undefined
  ): string {
    if (statut === null || statut === undefined) {
      return 'Non renseigné';
    }

    switch (Number(statut)) {
      case StatutFoncier.NonRenseigne:
        return 'Non renseigné';

      case StatutFoncier.TitreFoncierDisponible:
        return 'Titre foncier disponible';

      case StatutFoncier.DemandeEnCours:
        return 'Demande en cours';

      case StatutFoncier.SansTitreFoncier:
        return 'Sans titre foncier';

      case StatutFoncier.Autre:
        return 'Autre';

      default:
        return 'Non renseigné';
    }
  }

  selectionnerPhoto(index: number): void {
    if (index < 0 || index >= this.photos.length) {
      return;
    }

    this.photoActiveIndex = index;
  }

  allerVersListe(): void {
    this.router.navigate(['/biens']);
  }

  modifierBien(): void {
    if (!this.peutGererBien || !this.bien) {
      return;
    }

    this.router.navigate(['/biens'], {
      queryParams: {
        edit: this.bien.id
      }
    });
  }

  gererUnites(): void {
    if (!this.peutGererBien || !this.bien) {
      return;
    }

    this.router.navigate(['/uniteslocatives'], {
      queryParams: {
        bienId: this.bien.id
      }
    });
  }

  rechargerBien(): void {
    if (this.bienId) {
      this.chargerBien(this.bienId);
    }
  }
}