export interface BaseEntity {
  id: string;
  dateCreation?: string;
  estSupprime?: boolean;
  dateSuppression?: string | null;
}

// --- ENUMS C# ---
export enum TypeBien {
    Appartement = 1,
    Maison = 2,
    Villa = 3,
    Boutique = 4,
    Terrain = 5,
    Chambre =6,
    Studio = 7
}

export enum StatutBien {
  Disponible = 1,
  Loue = 2,
  EnTravaux = 4,
  Reserve = 3,
}

export enum RoleUtilisateur {
  Administrateur = 1,
  Gestionnaire = 2,
  Agent = 3,
  Locataire = 4
}

// Constantes textuelles pour les rôles normalisés
export const ROLES = {
  Administrateur: 'administrateur',
  Admin: 'admin',
  Gestionnaire: 'gestionnaire',
  Agent: 'agent',
  Locataire: 'locataire'
} as const;

export enum StatutContrat {
  EnAttente = 0,
  Actif = 1,
  Expire = 2,
  Resilie = 3,
}

export enum ModePaiement {
  OrangeMoney = 1,
  MtnMoney = 2,
  Especes = 3,
  M2u = 4,
  SaraMoney = 5
}

export enum StatutTransaction {
  EnAttente = 1,
  Confirme = 2,
  Echoue = 3
}

// --- DTOS ---
export interface UserPayload {
  id: string;
  nom: string;
  prenom: string;
  email: string;
  role: string;
}

export interface UtilisateurDto extends BaseEntity {
  nom: string;
  prenom: string;
  telephone: string;
  email: string;
  role: RoleUtilisateur;
  statut: boolean;
  derniereConnexion?: string;
  telephoneVerifie?: boolean;
}

export interface BienDto extends BaseEntity {
  reference: string;
  type: TypeBien;
  adresse: string;
  ville: string;
  quartier: string;
  superficie: number;
  loyer: number;
  caution?: number;
  statut: StatutBien;
  proprietaireId: string;
  proprietaireNom?: string;
  photos: string[];

  locataireActuel?: {
  contratId: string;
  nom: string;
  telephone: string;
  debutBail: string;
  finBail: string;
  };
  historiquePaiements?: Array<{
    id: string;
    date: string;
    montant: number;
    mode: string;
  }>;
}

export interface ContratDto extends BaseEntity {
  bienId: string;
  bien?: BienDto;
  locataireId: string;
  locataire?: UtilisateurDto;
  dateDebut: string;
  dateFin: string;
  montantLoyer: number;
  montantCaution: number;
  statut: StatutContrat;
  photos?: string[];
  frequencePaiement: number;
  delaiJoursTolerance: number;
}

export interface PaiementDto extends BaseEntity {
  contratId: string;
  contrat?: ContratDto;
  montant: number;
  datePaiement: string;
  mode: ModePaiement;
  statut: StatutTransaction;
  referenceTransactionOperateur?: string;
  numeroQuittance: string;
  bienReference?: string;
}

export interface DashboardStatsDto {
  biensDisponibles: number;
  loyersEncaissesMois: number;
  impayesEnCours: number;
  tauxOccupation: number;
  encaissementsGraph: {
    labels: string[];
    datasets: number[];
  };
  alertes: AlertItemDto[];
}

export interface AlertItemDto {
  id: number;
  message: string;
  type?: 'info' | 'warning' | 'danger';
}

export interface KpiCard {
  label: string;
  value: number | string;
  isCurrency?: boolean;
  unit?: string;
}

export interface NavItem {
  label: string;
  icon: string;
  route: string;
}

export interface PaiementListItem {
  id: string;
  numeroQuittance: string;
  bienReference?: string;
  bienVille?: string;
  locataireNom?: string;
  montant: number;
  modePaiement: ModePaiement;
  statutTransaction: StatutTransaction;
  dateCreation?: string;
  referenceTransactionOperateur?: string;
}

export interface PaiementFilterCriteria {
  recherche?: string;
  statutTransaction?: StatutTransaction;
  modePaiement?: ModePaiement;
  dateDebut?: string;
  dateFin?: string;
}

export enum StatutDemandeVisite {
  EnAttente = 1,
  Confirmee = 2,
  Annulee = 3,
  Effectuee = 4
}

export interface DemandeVisiteDto  {
  id?: string;
  bienId: string;
  bien?: BienDto;
  agentId?: string | null;
  agent?: UtilisateurDto | null;
  nomProspect: string;
  telephoneProspect: string;
  dateSouhaitee: string | Date;
  statut?: StatutDemandeVisite | number;
}