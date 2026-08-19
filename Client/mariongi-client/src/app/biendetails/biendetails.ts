import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { switchMap, takeWhile } from 'rxjs/operators';
import { of, interval, Subscription } from 'rxjs';

import { BiensService } from '../services/biens.service';
import { PaiementsService } from '../services/paiements.service';
import { ModePaiement } from '../models/gestimmo.models';
import { environment } from '../../environments/environment.development';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-bien-details',
  standalone: true,
  imports: [
    CommonModule, 
    DatePipe, 
    ButtonModule, 
    DialogModule, 
    SelectModule, 
    InputTextModule,
    ReactiveFormsModule
  ],
  providers: [MessageService],
  templateUrl: './biendetails.html',
  styleUrls: ['./biendetails.scss']
})
export class BienDetails implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  public readonly bienService = inject(BiensService);
  private readonly paiementsService = inject(PaiementsService);
  private readonly fb = inject(FormBuilder);
  paiementEspecesEnCours = false;
  bien: any = null;
  bienId: string | null = null;
  chargementEnCours: boolean = true;
  photoActiveIndex = 0;
  apiUrl = environment.apiUrl;
  serverUrl = environment.apiUrl.replace('/api', '');
  private readonly messageService = inject(MessageService);

  // ===== États des modales de Paiement =====
  displayChoixModePaiement = false;
  displayPaiementModal = false;
  displayPaiementEspecesModal = false;
  paiementEnCours = false;
  pollingSub?: Subscription;
  paiementEnAttenteId: string | null = null;

  // ===== Formulaires réactifs =====
  paiementForm: FormGroup = this.fb.group({
    montant: [null, [Validators.required, Validators.min(1)]],
    modePaiement: [null, Validators.required],
    telephone: ['', Validators.required],
  });

  paiementEspecesForm: FormGroup = this.fb.group({
    montant: [null, [Validators.required, Validators.min(1)]],
  });

  readonly modesPaiement = [
    { label: 'Orange Money', value: ModePaiement.OrangeMoney },
    { label: 'MTN Money', value: ModePaiement.MtnMoney },
    { label: 'M2U', value: ModePaiement.M2u },
    { label: 'SARA Money', value: ModePaiement.SaraMoney },
  ];

  ngOnInit(): void {
    this.route.paramMap.pipe(
      switchMap(params => {
        const id = params.get('id');
        this.bienId = id;
        this.chargementEnCours = true;
        if (id) {
          return this.bienService.getBienById(id);
        }
        return of(null);
      })
    ).subscribe({
      next: (data) => {
        this.bien = data;
        this.photoActiveIndex = 0;
        this.chargementEnCours = false;
      },
      error: (err) => {
        console.error("Erreur lors de la récupération du bien :", err);
        this.chargementEnCours = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.pollingSub?.unsubscribe();
  }

private rafraichirBien(): void {
    if (!this.bienId) return;
    this.bienService.getBienById(this.bienId).subscribe({
      next: (data) => { this.bien = data; },
      error: (err) => console.error('Erreur rafraîchissement bien:', err)
    });
  }

  allerVersListe(): void {
    this.router.navigate(['/biens']);
  }

  renouvelerContrat(): void {}
  resilierBail(): void {}

  genererLesQuittancesContrat(): void {
  const contratId = this.bien?.locataireActuel?.contratId;
  if (!contratId) return;

  this.paiementsService.telechargerToutesLesQuittances(contratId).subscribe({
    next: (blob) => {
      // 1. Forcer le type MIME zip
      const zipBlob = new Blob([blob], { type: 'application/zip' });
      const url = window.URL.createObjectURL(zipBlob);
      const a = document.createElement('a');
      a.href = url;
      
      // 2. S'assurer que l'extension est bien .zip
      a.download = `Quittances_Contrat_${contratId}.zip`;
      
      a.click();
      window.URL.revokeObjectURL(url);
    },
    error: (err) => {
      console.error("Erreur lors du téléchargement des quittances :", err);
    }
  });
}

  // ===== Paiement : déclenchement du choix =====
  enregistrerPaiement(): void {
    if (!this.bien?.locataireActuel?.contratId) {
      console.warn("Impossible d'initier un paiement : aucun contrat actif.");
      return;
    }
    this.displayChoixModePaiement = true;
  }

  choisirMobileMoney(): void {
    this.displayChoixModePaiement = false;
    this.paiementForm.reset();
    // Pré-remplir le montant avec le loyer du bien si disponible par défaut
    if (this.bien?.loyer) {
      this.paiementForm.patchValue({ montant: this.bien.loyer });
    }
    this.displayPaiementModal = true;
  }

  choisirEspeces(): void {
    this.displayChoixModePaiement = false;
    this.paiementEspecesForm.reset();
    if (this.bien?.loyer) {
      this.paiementEspecesForm.patchValue({ montant: this.bien.loyer });
    }
    this.displayPaiementEspecesModal = true;
  }

  // ===== Paiement : Mobile Money (soumission + polling) =====
  soumettrePaiement(): void {
    if (this.paiementForm.invalid || !this.bien?.locataireActuel?.contratId) return;

    this.paiementEnCours = true;
    const { montant, modePaiement, telephone } = this.paiementForm.value;

    this.paiementsService.initierPaiement({
      contratId: this.bien.locataireActuel.contratId,
      montant,
      modePaiement,
      telephone,
    }).subscribe({
      next: (res) => {
        this.paiementEnAttenteId = res.paiementId;
        this.demarrerPolling(res.paiementId);
      },
      error: (err) => {
        console.error('Erreur initiation paiement:', err);
        this.paiementEnCours = false;
      }
    });
  }

  private demarrerPolling(paiementId: string): void {
    let tentatives = 0;
    const maxTentatives = 20; // 20 x 3s = 60s max

    this.pollingSub = interval(3000).pipe(
      switchMap(() => this.paiementsService.getStatut(paiementId)),
      takeWhile((res) => {
        tentatives++;
        return res.statut === 'EnAttente' && tentatives < maxTentatives;
      }, true)
    ).subscribe({
      next: (res) => {
        if (res.statut !== 'EnAttente') {
          this.paiementEnCours = false;
          this.displayPaiementModal = false;
          this.paiementEnAttenteId = null;
          this.rafraichirBien();
        }
      },
      error: () => {
        this.paiementEnCours = false;
      }
    });
  }

// ===== Paiement : Espèces (validation immédiate) =====
soumettrePaiementEspeces(): void {
  if (this.paiementEspecesForm.invalid || !this.bien?.locataireActuel?.contratId || this.paiementEspecesEnCours) return;

  this.paiementEspecesEnCours = true; // <-- Bloque les clics multiples

  this.paiementsService.enregistrerPaiementEspeces({
    contratId: this.bien.locataireActuel.contratId,
    montant: this.paiementEspecesForm.value.montant,
  }).subscribe({
    next: () => {
      this.paiementEspecesEnCours = false;
      this.displayPaiementEspecesModal = false;
      
      // Message de succès
      this.messageService.add({ 
        severity: 'success', 
        summary: 'Succès', 
        detail: 'Le paiement en espèces a été enregistré avec succès.' 
      });

      this.rafraichirBien();
    },
    error: (err) => {
      console.error('Erreur paiement espèces:', err);
      this.paiementEspecesEnCours = false; // <-- Ne pas oublier de débloquer en cas d'erreur
      
      // Message d'erreur optionnel pour informer l'utilisateur
      this.messageService.add({ 
        severity: 'error', 
        summary: 'Erreur', 
        detail: err.error?.message || 'Erreur lors de l\'enregistrement du paiement.' 
      });
    }
  });
}

telechargerQuittance(paiementId: string): void {
  this.paiementsService.telechargerQuittanceBlob(paiementId);
}

}