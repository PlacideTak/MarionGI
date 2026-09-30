import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

// Modules PrimeNG
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select'; 
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';

// Services & Models
import { ContratsService } from '../services/contrats.service';
import { BiensService } from '../services/biens.service';
import { UtilisateursService } from '../services/utilisateurs.service';
import { ContratDto, ROLES, StatutContrat } from '../models/gestimmo.models';
import { AuthService } from '../login/auth.service';

interface DropdownItem {
  label: string;
  value: string;
}

@Component({
  selector: 'app-contrats',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    TagModule,
    ToastModule,
    ConfirmDialogModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './contrats.html',
  styleUrl: './contrats.scss'
})
export class Contrats implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly contratsService = inject(ContratsService);
  private readonly biensService = inject(BiensService);
  private readonly utilisateursService = inject(UtilisateursService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly authService = inject(AuthService);
  // Propriété pour vérifier les droits d'accès
  canCreateContrat: boolean = false;
  contrats: ContratDto[] = [];
  biensOptions: DropdownItem[] = [];
  locatairesOptions: DropdownItem[] = [];

  contratDialog: boolean = false;
  contratForm!: FormGroup;
  isEditMode: boolean = false;
  selectedContratId?: string;

  statutOptions = [
    { label: 'Actif', value: StatutContrat.Actif },
    { label: 'En Attente', value: StatutContrat.EnAttente },
    { label: 'Expiré', value: StatutContrat.Expire },
    { label: 'Résilié', value: StatutContrat.Resilie }
  ];

  ngOnInit(): void {
    this.initForm();
    this.chargerContrats();
    this.chargerListesDeroulantes();
    this.verifierPermissions();
  }

  private initForm(): void {
    this.contratForm = this.fb.group({
      bienId: ['', Validators.required],
      locataireId: ['', Validators.required],
      dateDebut: [null, Validators.required],
      dateFin: [null, Validators.required],
      montantLoyer: [null, [Validators.required, Validators.min(0)]],
      montantCaution: [0, [Validators.required, Validators.min(0)]],
      frequencePaiement: [1, [Validators.required, Validators.min(1)]],
      delaiJoursTolerance: [5, [Validators.required, Validators.min(0)]],
      statut: [StatutContrat.EnAttente, Validators.required]
    });
  }

  chargerContrats(): void {
    this.contratsService.getContrats().subscribe({
      next: (data) => {
        this.contrats = Array.isArray(data) ? [...data] : [];
      },
      error: (err) => {
        this.messageService.add({ 
          severity: 'error', 
          summary: 'Erreur HTTP', 
          detail: `Impossible de charger les contrats (${err.status || 'Erreur réseau'})` 
        });
      }
    });
  }

  chargerListesDeroulantes(): void {
    this.biensService.getBiens().subscribe({
      next: (biens) => {
        if (Array.isArray(biens)) {
          this.biensOptions = biens.map(b => ({
            label: `${b.reference} (${b.adresse || ''})`.trim() || `Bien ${b.id?.substring(0, 6)}`,
            value: b.id
          }));
        }
      },
      error: (err) => console.error('Erreur chargement biens:', err)
    });

    this.utilisateursService.getLocataires().subscribe({
      next: (locataires) => {
        if (Array.isArray(locataires)) {
          this.locatairesOptions = locataires.map(l => ({
            label: `${l.prenom || ''} ${l.nom || ''}`.trim() || ROLES.Locataire,
            value: l.id
          }));
        }
      },
      error: (err) => console.error('Erreur chargement locataires:', err)
    });
  }

  getShortId(id?: string): string {
    return id ? id.substring(0, 8).toUpperCase() : 'N/A';
  }

  getStatutLabel(statut: StatutContrat | number): string {
    switch (statut) {
      case StatutContrat.Actif:
      case 0:
        return 'Actif';
      case StatutContrat.EnAttente:
      case 1:
        return 'En Attente';
      case StatutContrat.Expire:
      case 2:
        return 'Expiré';
      case StatutContrat.Resilie:
      case 3:
        return 'Résilié';
      default:
        return 'Inconnu';
    }
  }

  getSeverity(statut: StatutContrat | number | string): 'success' | 'info' | 'warn' | 'danger' | undefined {
    switch (statut) {
      case StatutContrat.Actif:
      case 0:
      case 'Actif':
        return 'success';
      case StatutContrat.EnAttente:
      case 1:
      case 'EnAttente':
        return 'info';
      case StatutContrat.Expire:
      case 2:
      case 'Expiré':
        return 'warn';
      case StatutContrat.Resilie:
      case 3:
      case 'Resilié':
        return 'danger';
      default:
        return undefined;
    }
  }

  openNew(): void {
    this.isEditMode = false;
    this.selectedContratId = undefined;
    this.contratForm.reset({ 
      statut: StatutContrat.EnAttente, 
      montantCaution: 0,
      frequencePaiement: 1,
      delaiJoursTolerance: 5
    });
    this.contratDialog = true;
  }

  editContrat(contrat: ContratDto): void {
    this.isEditMode = true;
    this.selectedContratId = contrat.id;

    if (!this.selectedContratId) {
      this.messageService.add({
        severity: 'error',
        summary: 'Erreur',
        detail: 'L\'identifiant du contrat est introuvable.'
      });
      return;
    }
    
    const formattedDateDebut = contrat.dateDebut ? new Date(contrat.dateDebut).toISOString().substring(0, 10) : null;
    const formattedDateFin = contrat.dateFin ? new Date(contrat.dateFin).toISOString().substring(0, 10) : null;

    this.contratForm.patchValue({
      bienId: contrat.bienId || contrat.bien?.id,
      locataireId: contrat.locataireId || contrat.locataire?.id,
      dateDebut: formattedDateDebut,
      dateFin: formattedDateFin,
      montantLoyer: contrat.montantLoyer,
      montantCaution: contrat.montantCaution,
      frequencePaiement: contrat.frequencePaiement,
      delaiJoursTolerance: contrat.delaiJoursTolerance,
      statut: contrat.statut
    });

    this.contratDialog = true;
  }

  saveContrat(): void {
    if (this.contratForm.invalid) {
      this.contratForm.markAllAsTouched();
      return;
    }

    const formValue = this.contratForm.value;

    const payload: Partial<ContratDto> = {
      bienId: formValue.bienId,
      locataireId: formValue.locataireId,
      dateDebut: new Date(formValue.dateDebut).toISOString(),
      dateFin: new Date(formValue.dateFin).toISOString(),
      montantLoyer: Number(formValue.montantLoyer),
      montantCaution: Number(formValue.montantCaution),
      frequencePaiement: Number(formValue.frequencePaiement || 1),
      delaiJoursTolerance: Number(formValue.delaiJoursTolerance || 0),
      statut: Number(formValue.statut)
    };

    if (this.isEditMode) {
      if (!this.selectedContratId) {
        this.messageService.add({ 
          severity: 'error', 
          summary: 'Erreur', 
          detail: 'L\'identifiant du contrat est manquant pour la modification.' 
        });
        return;
      }

      payload.id = this.selectedContratId;

      this.contratsService.updateContrat(this.selectedContratId, payload as ContratDto).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Succès', detail: 'Contrat mis à jour' });
          this.chargerContrats();
          this.contratDialog = false;
        },
        error: (err) => this.traiterErreurHttp(err, 'modification')
      });
    } else {
      this.contratsService.createContrat(payload as ContratDto).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Succès', detail: 'Contrat créé' });
          this.chargerContrats();
          this.contratDialog = false;
        },
        error: (err) => this.traiterErreurHttp(err, 'création')
      });
    }
  }

  deleteContrat(contrat: ContratDto): void {
    if (!contrat.id) return;

    this.confirmationService.confirm({
      message: `Voulez-vous vraiment supprimer ce contrat ?`,
      header: 'Confirmation de suppression',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Oui, supprimer',
      rejectLabel: 'Annuler',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.contratsService.deleteContrat(contrat.id!).subscribe({
          next: () => {
            this.messageService.add({ severity: 'success', summary: 'Succès', detail: 'Contrat supprimé' });
            this.chargerContrats();
          },
          error: (err) => this.traiterErreurHttp(err, 'suppression')
        });
      }
    });
  }

  private verifierPermissions(): void {
    this.canCreateContrat = this.authService.hasRole([ROLES.Administrateur, ROLES.Admin, ROLES.Gestionnaire]);
  }

  private traiterErreurHttp(err: any, action: 'création' | 'modification' | 'suppression'): void {
    console.error(`Erreur lors de la ${action}:`, err);

    if (err.status === 403) {
      this.messageService.add({ 
        severity: 'warn', 
        summary: 'Accès Refusé', 
        detail: `Vous n'avez pas les droits nécessaires pour la ${action} d'un contrat.`,
        life: 5000 
      });
    } else if (err.status === 401) {
      this.messageService.add({ 
        severity: 'error', 
        summary: 'Non Authentifié', 
        detail: 'Votre session a expiré. Veuillez vous reconnecter.' 
      });
    } else {
      this.messageService.add({ 
        severity: 'error', 
        summary: 'Erreur', 
        detail: `Une erreur est survenue lors de la ${action} (Code ${err.status || 'inconnu'}).` 
      });
    }
  }
}