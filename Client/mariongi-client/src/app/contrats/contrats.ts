import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { SelectModule } from 'primeng/select'; 
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MessageService, ConfirmationService } from 'primeng/api';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ContratsService } from '../services/contrats.service';
import { BiensService } from '../services/biens.service';
import { UtilisateursService } from '../services/utilisateurs.service';
import { ContratDto, StatutContrat, BienDto } from '../models/gestimmo.models';

@Component({
  selector: 'app-contrats',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    SelectModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    InputNumberModule,
    ToastModule,
    ConfirmDialogModule,
    TagModule,
    TooltipModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './contrats.html',
  styleUrls: ['./contrats.scss']
})
export class Contrats implements OnInit, OnDestroy {
  private readonly contratsService = inject(ContratsService);
  private readonly biensService = inject(BiensService);
  private readonly utilisateurService = inject(UtilisateursService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly router = inject(Router);
  
  private routerSub!: Subscription;

  contrats: ContratDto[] = [];
  biensDisponibles: any[] = [];
  locataires: any[] = [];
  chargement = false;
  
  displayModal = false;
  isEditMode = false;
  selectedContratId: string | null = null;
  contratForm!: FormGroup;

  statutsContrat = [
    { label: 'Actif', value: StatutContrat.Actif },
    { label: 'Résilié', value: StatutContrat.Resilie },
    { label: 'Expiré', value: StatutContrat.Expire },
    { label: 'En Attente', value: StatutContrat.EnAttente }
  ];

  frequencesPaiement = [
    { label: 'Mensuel', value: 1 },
    { label: 'Trimestriel', value: 3 },
    { label: 'Annuel', value: 12 }
  ];

  ngOnInit(): void {
    this.initForm();
    this.chargerDonnees();

    this.routerSub = this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.chargerDonnees();
    });
  }

  ngOnDestroy(): void {
    if (this.routerSub) {
      this.routerSub.unsubscribe();
    }
  }

  initForm(): void {
    this.contratForm = this.fb.group({
      bienId: ['', Validators.required],
      locataireId: ['', Validators.required],
      dateDebut: ['', Validators.required],
      dateFin: ['', Validators.required],
      montantLoyer: [0, [Validators.required, Validators.min(0)]],
      montantCaution: [0, [Validators.required, Validators.min(0)]],
      statut: [StatutContrat.Actif, Validators.required],
      frequencePaiement: [1, Validators.required],
      delaiJoursTolerance: [5, [Validators.required, Validators.min(0)]]
    });
  }

  chargerDonnees(): void {
    this.chargerContrats();
    this.chargerBiens();
    this.chargerLocataires();
  }

  chargerContrats(): void {
    this.chargement = true;
    this.contratsService.getContrats().subscribe({
      next: (data) => {
        this.contrats = data || [];
        this.chargement = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.chargement = false;
        this.messageService.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de charger les contrats.' });
        this.cdr.detectChanges();
      }
    });
  }

  chargerBiens(): void {
    this.biensService.getBiens().subscribe({
      next: (data: BienDto[]) => {
 
        this.biensDisponibles = data.map(b => ({
          label: `${b.reference} - ${b.ville} (${b.quartier})`,
          value: b.id
        }));
        this.cdr.detectChanges();
      },
      error: (err) => console.error("Erreur chargement biens", err)
    });
  }

  chargerLocataires(): void {
    this.utilisateurService.getUtilisateurs().subscribe({
      next: (data: any) => {
        const liste = Array.isArray(data) ? data : (data.items || []);
        
        this.locataires = data.map((u: any) => ({
          label: `${u.prenom} ${u.nom} (${u.email})`,
          value: u.id
        }));
        this.cdr.detectChanges();
      },
      error: (err) => console.error("Erreur chargement locataires", err)
    });
  }

  ouvrirModalAjout(): void {
    this.isEditMode = false;
    this.selectedContratId = null;
    this.contratForm.reset({
      statut: StatutContrat.Actif,
      montantLoyer: 0,
      montantCaution: 0,
      frequencePaiement: 1,
      delaiJoursTolerance: 5
    });
    this.displayModal = true;
  }

  ouvrirModalModification(contrat: ContratDto): void {
    this.isEditMode = true;
    this.selectedContratId = contrat.id;
    this.contratForm.patchValue({
      bienId: contrat.bienId,
      locataireId: contrat.locataireId,
      dateDebut: contrat.dateDebut ? contrat.dateDebut.split('T')[0] : '',
      dateFin: contrat.dateFin ? contrat.dateFin.split('T')[0] : '',
      montantLoyer: contrat.montantLoyer,
      montantCaution: contrat.montantCaution,
      statut: contrat.statut,
      frequencePaiement: contrat.frequencePaiement,
      delaiJoursTolerance: contrat.delaiJoursTolerance
    });
    this.displayModal = true;
  }

  sauvegarder(): void {
  if (this.contratForm.invalid) {
    this.contratForm.markAllAsTouched();
    return;
  }

  const valeurs = this.contratForm.value;
  const aujourdhui = new Date();
  aujourdhui.setHours(0, 0, 0, 0); // Réinitialiser l'heure pour comparer uniquement les dates

  const debut = new Date(valeurs.dateDebut);
  const fin = new Date(valeurs.dateFin);

  // 1. Vérification : Pas de date passée
  /*if (debut < aujourdhui) {
    this.messageService.add({ 
      severity: 'error', 
      summary: 'Date invalide', 
      detail: 'La date de début du contrat ne peut pas être dans le passé.' 
    });
    return;
  }*/

  // 2. Vérification : Date de fin > Date de début
  if (fin <= debut) {
    this.messageService.add({ 
      severity: 'error', 
      summary: 'Date invalide', 
      detail: 'La date de fin doit être postérieure à la date de début.' 
    });
    return;
  }

  // 3. Calcul de la durée en jours pour la cohérence de fréquence
  const diffTime = Math.abs(fin.getTime() - debut.getTime());
  const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

  const joursRequis = valeurs.frequencePaiement * 30; 

  if (diffDays < joursRequis) {
    this.messageService.add({ 
      severity: 'warn', 
      summary: 'Incohérence', 
      detail: `La durée du contrat (${diffDays} jours) est trop courte pour une fréquence de paiement de ${joursRequis} jours.` 
    });
    return;
  }

  // 4. Suite de la logique (Create / Update)
  if (this.isEditMode && this.selectedContratId) {
    const payload: ContratDto = { id: this.selectedContratId, ...valeurs };
    this.contratsService.updateContrat(this.selectedContratId, payload).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Succès', detail: 'Contrat modifié avec succès.' });
        this.displayModal = false;
        this.chargerDonnees();
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: 'Erreur', detail: err.error?.message || 'Erreur lors de la modification.' });
      }
    });
  } else {
    this.contratsService.createContrat(valeurs).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Succès', detail: 'Contrat créé avec succès.' });
        this.displayModal = false;
        this.chargerDonnees();
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: 'Erreur', detail: err.error?.message || 'Erreur lors de la création.' });
      }
    });
  }
}
  supprimerContrat(contrat: ContratDto): void {
    this.confirmationService.confirm({
      message: `Voulez-vous vraiment résilier et archiver ce contrat ?`,
      header: 'Confirmation de résiliation',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Oui',
      rejectLabel: 'Non',
      accept: () => {
        this.contratsService.deleteContrat(contrat.id).subscribe({
          next: () => {
            this.messageService.add({ severity: 'success', summary: 'Succès', detail: 'Contrat résilié avec succès.' });
            this.chargerDonnees();
          },
          error: () => {
            this.messageService.add({ severity: 'error', summary: 'Erreur', detail: "Erreur lors de la résiliation." });
          }
        });
      }
    });
  }

  getStatutSeverity(statut: StatutContrat): 'success' | 'info' | 'warn' | 'danger' {
    switch (statut) {
      case StatutContrat.Actif: return 'success';
      case StatutContrat.Resilie: return 'danger';
      case StatutContrat.Expire: return 'warn';
      default: return 'info';
    }
  }

  getStatutLibelle(statut: StatutContrat): string {
    const found = this.statutsContrat.find(s => s.value === statut);
    return found ? found.label : 'Inconnu';
  }
}