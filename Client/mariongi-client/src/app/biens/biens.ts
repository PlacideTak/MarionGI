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
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { BiensService } from '../services/biens.service';
import { UtilisateursService } from '../services/utilisateurs.service';
import { AuthService } from '../login/auth.service';
import { BienDto, TypeBien, StatutBien } from '../models/gestimmo.models';
import { environment } from '../../environments/environment.development';

@Component({
  selector: 'app-biens',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    SelectModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    ToastModule,
    ConfirmDialogModule,
    TagModule,
    TooltipModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './biens.html',
  styleUrls: ['./biens.scss']
})
export class Biens implements OnInit, OnDestroy {
  readonly bienService = inject(BiensService);
  private readonly utilisateurService = inject(UtilisateursService);
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly router = inject(Router);
  
  readonly apiUrl = environment.apiUrl.replace('/api', '');
  private routerSub!: Subscription;

  biens: BienDto[] = [];
  proprietaires: any[] = [];
  chargement = false;
  
  userRole: string = ''; // 👈 AJOUTEZ CETTE LIGNE ICI

  get canManageBiens(): boolean {
    return this.authService.hasRole(['Administrateur', 'Admin', 'Gestionnaire']);
  }

  get estAgentCommercial(): boolean {
    return this.authService.hasRole(['Agent']);
  }

  // Le reste de votre code...
  // Gestion de la modale
  displayModal = false;
  isEditMode = false;
  selectedBienId: string | null = null;
  bienForm!: FormGroup;

  // Gestion des fichiers et aperçus photos
  fichiersSelectionnes: File[] = [];
  apercusImages: string[] = [];
  photosExistantes: string[] = []; 
  statutsBien: any[] = [];
  typesBien: any[] = [];

  ngOnInit(): void {
    // Récupération directe du rôle via le signal currentUser de l'AuthService
    this.userRole = this.authService.currentUser()?.role || '';
    
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
    this.bienForm = this.fb.group({
      reference: [''],
      type: [TypeBien.Appartement, Validators.required],
      adresse: ['', Validators.required],
      ville: ['', Validators.required],
      quartier: ['', Validators.required],
      superficie: [0, [Validators.required, Validators.min(1)]],
      loyer: [0, [Validators.required, Validators.min(0)]],
      statut: [StatutBien.Disponible, Validators.required],
      proprietaireId: ['', Validators.required]
    });
  }

  chargerDonnees(): void {
    this.chargerBiens();
    if (this.canManageBiens) {
      this.chargerProprietaires();
    }
  }

  chargerBiens(): void {
    this.chargement = true;
    this.bienService.getBiens().subscribe({
      next: (data) => {
        this.biens = data || [];
        this.chargement = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.chargement = false;
        this.messageService.add({ severity: 'error', summary: 'Erreur', detail: 'Impossible de charger la liste des biens.' });
        this.cdr.detectChanges();
      }
    });
  }

  chargerProprietaires(): void {
    this.utilisateurService.getUtilisateurs().subscribe({
      next: (data: any) => {
        const liste = Array.isArray(data) ? data : (data.items || []);
        this.proprietaires = liste.map((u: any) => ({
          label: `${u.prenom} ${u.nom} (${u.email})`,
          value: u.id
        }));
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error("Erreur chargement propriétaires", err);
      }
    });
  }

  surSelectionFichiers(event: any): void {
    if (!this.canManageBiens) return;
    const files: FileList = event.target.files;
    if (files) {
      for (let i = 0; i < files.length; i++) {
        const file = files[i];
        this.fichiersSelectionnes.push(file);

        const reader = new FileReader();
        reader.onload = (e: any) => {
          this.apercusImages.push(e.target.result);
          this.cdr.detectChanges();
        };
        reader.readAsDataURL(file);
      }
    }
    event.target.value = '';
  }

  supprimerImagePrevisualisee(index: number): void {
    this.fichiersSelectionnes.splice(index, 1);
    this.apercusImages.splice(index, 1);
  }

  supprimerPhotoExistante(index: number): void {
    this.photosExistantes.splice(index, 1);
  }

  ouvrirModalAjout(): void {
    if (!this.canManageBiens) return;
    this.isEditMode = false;
    this.selectedBienId = null;
    this.fichiersSelectionnes = [];
    this.apercusImages = [];
    this.photosExistantes = [];
    this.bienForm.reset({ 
      type: TypeBien.Appartement, 
      statut: StatutBien.Disponible, 
      superficie: 0, 
      loyer: 0 
    });
    this.displayModal = true;
  }

  ouvrirModalModification(bien: BienDto): void {
    if (!this.canManageBiens) return;
    this.isEditMode = true;
    this.selectedBienId = bien.id;
    this.fichiersSelectionnes = [];
    this.apercusImages = [];
    this.photosExistantes = bien.photos ? [...bien.photos] : [];

    this.bienForm.patchValue({
      reference: bien.reference,
      type: bien.type,
      adresse: bien.adresse,
      ville: bien.ville,
      quartier: bien.quartier,
      superficie: bien.superficie,
      loyer: bien.loyer,
      statut: bien.statut,
      proprietaireId: bien.proprietaireId
    });
    this.displayModal = true;
  }

  sauvegarder(): void {
    if (!this.canManageBiens) return;
    if (this.bienForm.invalid) {
      this.bienForm.markAllAsTouched();
      return;
    }

    const formData = new FormData();
    const valeurs = this.bienForm.value;

    Object.keys(valeurs).forEach(cle => {
      if (valeurs[cle] !== null && valeurs[cle] !== undefined) {
        formData.append(cle, valeurs[cle]);
      }
    });

    this.photosExistantes.forEach(photo => {
      formData.append('photosExistantes', photo);
    });

    this.fichiersSelectionnes.forEach(file => {
      formData.append('fichiers', file, file.name);
    });

    if (this.isEditMode && this.selectedBienId) {
      this.bienService.updateBien(this.selectedBienId, formData).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Succès', detail: 'Bien modifié avec succès.' });
          this.displayModal = false;
          this.chargerDonnees();
        },
        error: (err) => {
          const errorMessage = err.error?.message || '';
          if (err.status === 409 || errorMessage.toLowerCase().includes('reference') || errorMessage.toLowerCase().includes('existe déjà')) {
            this.messageService.add({ severity: 'error', summary: 'Référence existante', detail: 'Un bien possède déjà cette référence. Veuillez en choisir une autre.' });
          } else {
            this.messageService.add({ severity: 'error', summary: 'Erreur', detail: errorMessage || 'Erreur lors de la modification.' });
          }
        }
      });
    } else {
      this.bienService.createBien(formData).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Succès', detail: 'Bien créé avec succès.' });
          this.displayModal = false;
          this.chargerDonnees();
        },
        error: (err) => {
          const errorMessage = err.error?.message || '';
          if (err.status === 409 || errorMessage.toLowerCase().includes('reference') || errorMessage.toLowerCase().includes('existe déjà')) {
            this.messageService.add({ severity: 'error', summary: 'Référence existante', detail: 'Un bien possède déjà cette référence. Veuillez en choisir une autre.' });
          } else {
            this.messageService.add({ severity: 'error', summary: 'Erreur', detail: errorMessage || 'Erreur lors de la création.' });
          }
        }
      });
    }
  }

  supprimerBien(bien: BienDto): void {
    if (!this.canManageBiens) return;
    this.confirmationService.confirm({
      message: `Voulez-vous vraiment archiver le bien ${bien.reference} ?`,
      header: 'Confirmation de suppression',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Oui',
      rejectLabel: 'Non',
      accept: () => {
        this.bienService.deleteBien(bien.id).subscribe({
          next: () => {
            this.messageService.add({ severity: 'success', summary: 'Succès', detail: 'Bien archivé avec succès.' });
            this.chargerDonnees();
          },
          error: () => {
            this.messageService.add({ severity: 'error', summary: 'Erreur', detail: "Erreur lors de la suppression." });
          }
        });
      }
    });
  }
}