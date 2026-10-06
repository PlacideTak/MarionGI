// ============================================================
// BASE ENTITY
// ============================================================

export interface BaseEntity {
  id: string;
  dateCreation?: string;
  estSupprime?: boolean;
  dateSuppression?: string | null;
}


// ============================================================
// ENUMS
// ============================================================

export enum TypeBien {
  Immeuble = 1,
  Maison = 2,
  Villa = 3,
  Terrain = 4,
  Boutique = 5,
  Magasin = 6,
  Bureau = 7,
  Entrepot = 8,
  Parking = 9,
  Autre = 10
}


/**
 * Le statut concerne principalement les unités locatives.
 *
 * Le BienImmobilier lui-même ne possède plus de statut.
 *
 * Le nom StatutBien est conservé afin de rester compatible
 * avec l'enum C# actuel si celui-ci porte encore ce nom.
 */
export enum StatutBien {
  Disponible = 1,
  Loue = 2,
  Reserve = 3,
  EnTravaux = 4
}


export enum RoleUtilisateur {
  Administrateur = 1,
  Gestionnaire = 2,
  Agent = 3,
  Locataire = 4
}


// ============================================================
// RÔLES
// ============================================================

export const ROLES = {
  Administrateur: 'administrateur',
  Admin: 'admin',
  Gestionnaire: 'gestionnaire',
  Agent: 'agent',
  Locataire: 'locataire'
} as const;


// ============================================================
// STATUT CONTRAT
// ============================================================

export enum StatutContrat {
  EnAttente = 0,
  Actif = 1,
  Expire = 2,
  Resilie = 3
}


// ============================================================
// PAIEMENT
// ============================================================

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


// ============================================================
// DEMANDE DE VISITE
// ============================================================

export enum StatutDemandeVisite {
  EnAttente = 1,
  Confirmee = 2,
  Annulee = 3,
  Effectuee = 4
}


// ============================================================
// TYPES D'UNITÉS LOCATIVES
// ============================================================

export enum TypeUniteLocative {
  Appartement = 1,
  Maison = 2,
  Villa = 3,
  Boutique = 4,
  Terrain = 5,
  Chambre = 6,
  Studio = 7,
  Magasin = 8,
  Bureau = 9,
  Duplex = 10,
  Bungalow = 11,
  Penthouse = 12,
  Entrepot = 13,
  Parking = 14,
  Cave = 15
}


// ============================================================
// USER PAYLOAD
// ============================================================

export interface UserPayload {
  id: string;
  nom: string;
  prenom: string;
  email: string;
  role: string;
  societeId?: string;
}


// ============================================================
// UTILISATEUR
// ============================================================

export interface UtilisateurDto extends BaseEntity {

  nom: string;

  prenom: string;

  telephone: string;

  email: string;

  role: RoleUtilisateur;

  statut: boolean;

  derniereConnexion?: string;

  telephoneVerifie?: boolean;

  societeId: string;

  societeNom?: string;
}


export interface CreerUtilisateurRequest {

  nom: string;

  prenom: string;

  telephone?: string | null;

  email: string;

  role: RoleUtilisateur;

  motDePasseInitial: string;

  societeId?: string | null;
}


export interface ModifierUtilisateurRequest {

  nom: string;

  prenom: string;

  telephone?: string | null;

  email: string;

  role: RoleUtilisateur;

  statut: boolean;

  nouveauMotDePasse?: string | null;

  societeId?: string | null;
}


// ============================================================
// BIEN IMMOBILIER
// ============================================================

/**
 * Bien immobilier.
 *
 * Architecture :
 *
 * Société
 *    ↓
 * BienImmobilier
 *    ↓
 * UnitesLocatives
 *
 * Le bien immobilier ne possède plus de statut.
 * Le statut est géré au niveau de l'unité locative.
 */
export interface BienDto extends BaseEntity {

  reference: string;

  type: TypeBien;

  adresse: string;

  ville: string;

  quartier?: string | null;

  superficie: number;

  /**
   * Nom du bien immobilier.
   *
   * Exemple :
   * "Immeuble Marion"
   */
  nom: string;

  societeId: string;

  societeNom?: string;

  nombreUnites?: number;

  photos?: string[];
}


export interface BienDetailDto extends BienDto {

  unitesLocatives: UniteLocativeDto[];
}


// ============================================================
// CRÉATION D'UN BIEN
// ============================================================

export interface CreerBienRequest {

  reference: string;

  type: TypeBien;

  adresse: string;

  ville: string;

  quartier?: string | null;

  superficie: number;

  nom: string;

  /**
   * Nouvelles photos à téléverser.
   */
  fichiers?: File[];
}


// ============================================================
// MODIFICATION D'UN BIEN
// ============================================================

export interface ModifierBienRequest {

  reference: string;

  type: TypeBien;

  adresse: string;

  ville: string;

  quartier?: string | null;

  superficie: number;

  nom: string;

  /**
   * Photos existantes conservées.
   */
  photosExistantes?: string[];

  /**
   * Nouvelles photos à téléverser.
   */
  fichiers?: File[];
}


// ============================================================
// UNITÉ LOCATIVE
// ============================================================

export interface UniteLocativeDto extends BaseEntity {

  reference: string;

  type: TypeUniteLocative;

  superficie: number;

  loyer: number;

  /**
   * Statut de l'unité locative.
   */
  statut: StatutBien;

  /**
   * Bien immobilier auquel appartient l'unité.
   */
  bienImmobilierId: string;

  photos: string[];


  // ----------------------------------------------------------
  // Informations du bien parent
  // ----------------------------------------------------------

  bienReference?: string;

  /**
   * Nom du bien parent.
   *
   * Exemple :
   * "Immeuble Marion"
   */
  bienNom?: string;

  bienAdresse?: string;

  bienVille?: string;

  bienQuartier?: string | null;


  // ----------------------------------------------------------
  // Informations société
  // ----------------------------------------------------------

  societeId?: string;

  societeNom?: string;


  // ----------------------------------------------------------
  // Contrats
  // ----------------------------------------------------------

  /**
   * Contrats associés à l'unité.
   *
   * Cette propriété permet notamment de déterminer
   * si l'unité possède actuellement un contrat actif.
   */
  contrats?: ContratUniteDto[];

  /**
   * Indique directement si l'unité possède un contrat actif.
   *
   * Ce champ devrait idéalement être calculé par l'API.
   *
   * true  = au moins un contrat avec StatutContrat.Actif
   * false = aucun contrat actif
   */
  aContratActif?: boolean;


  // ----------------------------------------------------------
  // Statistiques
  // ----------------------------------------------------------

  nombreContrats?: number;

  nombreDemandesVisite?: number;
}


// ============================================================
// DÉTAIL D'UNE UNITÉ LOCATIVE
// ============================================================

export interface UniteLocativeDetailDto extends UniteLocativeDto {

  bien?: {

    id: string;

    reference: string;

    /**
     * Nom du bien immobilier.
     */
    nom: string;

    adresse: string;

    ville: string;

    quartier?: string | null;

    societeId: string;
  };

  /**
   * Contrats détaillés de l'unité.
   */
  contrats?: ContratUniteDto[];

  nombreDemandesVisite?: number;
}

export interface ContratUniteDto {

  id: string;

  /**
   * Référence lisible du contrat.
   *
   * Exemple :
   * "CTR-2026-001"
   */
  reference: string;

  statut: StatutContrat;

  dateDebut: string;

  dateFin?: string | null;

  montantLoyer: number;
}

// ============================================================
// CRÉATION D'UNE UNITÉ LOCATIVE
// ============================================================

export interface CreerUniteLocativeRequest {

  reference: string;

  type: TypeUniteLocative;

  superficie: number;

  loyer: number;

  statut: StatutBien;

  bienImmobilierId: string;

  /**
   * Nouvelles photos.
   *
   * Les fichiers sont envoyés séparément
   * via FormData.
   */
  fichiers?: File[];
}


// ============================================================
// MODIFICATION D'UNE UNITÉ LOCATIVE
// ============================================================

export interface ModifierUniteLocativeRequest {

  reference: string;

  type: TypeUniteLocative;

  superficie: number;

  loyer: number;

  statut: StatutBien;

  bienImmobilierId: string;

  /**
   * Photos existantes conservées.
   */
  photosExistantes?: string[];

  /**
   * Nouvelles photos.
   */
  fichiers?: File[];
}


// ============================================================
// MODIFICATION DU STATUT D'UNE UNITÉ
// ============================================================

export interface ModifierStatutUniteRequest {

  statut: StatutBien;
}


export interface ModifierStatutUniteResponse {

  message: string;

  id: string;

  statut: StatutBien;
}


// ============================================================
// SUPPRESSION D'UNE UNITÉ
// ============================================================

export interface SupprimerUniteResponse {

  message: string;
}


export interface ContratDto {

  id: string;

  /**
   * Référence unique et lisible du contrat.
   *
   * Exemple :
   * "CTR-2026-001"
   */
  reference: string;

  /**
   * Identifiant de l'unité locative.
   */
  uniteLocativeId: string;

  /**
   * Identifiant du locataire.
   */
  locataireId: string;

  dateDebut: string;

  dateFin: string;

  montantLoyer: number;

  montantCaution: number;

  frequencePaiement: number;

  delaiJoursTolerance: number;

  statut: StatutContrat;

  // ----------------------------------------------------------
  // Unité locative
  // ----------------------------------------------------------

  uniteLocative?: UniteLocativeDto | null;

  // ----------------------------------------------------------
  // Locataire
  // ----------------------------------------------------------

  locataire?: UtilisateurDto | null;
}

export interface CreerContratRequest {

  /**
   * Référence saisie par l'utilisateur.
   *
   * Exemple :
   * "CTR-2026-001"
   */
  reference: string;

  uniteLocativeId: string;

  locataireId: string;

  dateDebut: string;

  dateFin: string;

  montantLoyer: number;

  montantCaution: number;

  frequencePaiement: number;

  delaiJoursTolerance: number;
}

export interface ModifierContratRequest {

  dateDebut: string;

  dateFin: string;

  montantLoyer: number;

  montantCaution: number;

  frequencePaiement: number;

  delaiJoursTolerance: number;

  statut: StatutContrat;
}

// ============================================================
// PAIEMENT
// ============================================================

export interface PaiementDto extends BaseEntity {

  contratId: string;

  contrat?: ContratDto | null;

  montant: number;

  datePaiement: string;

  mode: ModePaiement;

  statut: StatutTransaction;

  referenceTransactionOperateur?: string;

  numeroQuittance: string;

  /**
   * Référence technique du bien.
   */
  bienReference?: string;

  /**
   * Nom affiché du bien.
   *
   * Exemple :
   * "Immeuble Marion"
   */
  bienNom?: string;
}


// ============================================================
// LISTE DES PAIEMENTS
// ============================================================

export interface PaiementListItem {

  id: string;

  datePaiement: string;

  montant: number;

  mode: ModePaiement;

  statut: StatutTransaction;

  numeroQuittance: string;

  locataireNom: string;

  uniteReference: string;

  /**
   * Nom du bien affiché dans la liste.
   */
  bienNom: string;
}


// ============================================================
// FILTRES PAIEMENTS
// ============================================================

export interface PaiementFilterCriteria {

  recherche?: string;

  statutTransaction?: StatutTransaction;

  modePaiement?: ModePaiement;

  dateDebut?: string;

  dateFin?: string;
}


// ============================================================
// DEMANDE DE VISITE
// ============================================================

export interface DemandeVisiteDto {

  id?: string;

  // =========================================================
  // UNITÉ LOCATIVE
  // =========================================================

  uniteLocativeId: string;

  uniteLocative?: UniteLocativeDto | null;


  // =========================================================
  // BIEN IMMOBILIER
  // =========================================================

  bienId?: string;

  bien?: BienDto | null;


  // =========================================================
  // AGENT
  // =========================================================

  agentId?: string | null;

  agent?: UtilisateurDto | null;


  // =========================================================
  // PROSPECT
  // =========================================================

  nomProspect: string;

  telephoneProspect: string;


  // =========================================================
  // VISITE
  // =========================================================

  dateSouhaitee: string | Date;

  observations?: string | null;


  // =========================================================
  // STATUT
  // =========================================================

  statut?: StatutDemandeVisite | number;
}


// ============================================================
// SOCIÉTÉ
// ============================================================

export interface SocieteDto extends BaseEntity {

  nom: string;

  numeroEntreprise?: string | null;

  adresse?: string | null;

  ville?: string | null;

  codePostal?: string | null;

  telephone?: string | null;

  email?: string | null;

  actif: boolean;

  nombreUtilisateurs: number;

  nombreBiens: number;
}


export interface ModifierSocieteRequest {

  nom: string;

  numeroEntreprise?: string | null;

  adresse?: string | null;

  ville?: string | null;

  codePostal?: string | null;

  telephone?: string | null;

  email?: string | null;
}


export interface ModifierStatutSocieteRequest {

  actif: boolean;
}


// ============================================================
// DASHBOARD
// ============================================================

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


// ============================================================
// RAPPORTS - DASHBOARD KPI
// ============================================================

export interface EncaissementsGraphDto {

  labels: string[];

  datasets: number[];
}


export interface AlerteRapportDto {

  id: string;

  titre: string;

  message: string;

  severite: 'danger' | 'warning' | 'info';

  type: string;

  contratId?: string;

  uniteReference?: string;

  bienNom?: string;

  dateFin?: string;

  joursDepuisExpiration?: number;

  joursAvantExpiration?: number;

  montantLoyer?: number;

  montantPaye?: number;

  solde?: number;

  dateLimite?: string;

  joursDeRetard?: number;
}


export interface DerniereUniteDto {

  id: string;

  reference: string;

  type: TypeUniteLocative;

  superficie: number;

  loyer: number;

  statut: StatutBien;

  dateCreation: string;

  bien: {

    id: string;

    reference: string;

    nom: string;

  };
}


export interface ImpayeDto {

  contratId: string;

  uniteReference: string;

  bienNom: string;

  bienReference: string;

  locataireNom: string;

  locataireTelephone: string;

  montantLoyer: number;

  montantPaye: number;

  solde: number;

  delaiToleranceJours: number;

  dateLimite: string;

  joursDeRetard: number;
}


export interface DashboardKpiDto {

  totalBiens: number;

  totalUnites: number;

  unitesLouees: number;

  unitesDisponibles: number;

  tauxOccupation: number;

  encaissementsMois: number;

  encaissementsGraph: EncaissementsGraphDto;

  contratsActifs: number;

  contratsExpires: number;

  nombreImpayes: number;

  montantImpayes: number;

  alertes: AlerteRapportDto[];

  impayes: ImpayeDto[];

  dernieresUnites: DerniereUniteDto[];
}


// ============================================================
// RAPPORT - ENCAISSEMENTS
// ============================================================

export interface EncaissementDetailDto {

  id: string;

  datePaiement: string;

  montant: number;

  modePaiement: string;

  uniteReference: string;

  bienNom: string;

  bienReference: string;

  locataireNom: string;
}


export interface EncaissementParModeDto {

  modePaiement: string;

  totalRecette: number;

  nombreTransactions: number;

  details: EncaissementDetailDto[];
}


export interface EncaissementsParModeResponseDto {

  dateDebut?: string;

  dateFin?: string;

  total: number;

  nombreTransactions: number;

  parMode: EncaissementParModeDto[];
}


// ============================================================
// RAPPORT - HISTORIQUE LOCATAIRE
// ============================================================

export interface HistoriquePaiementDto {

  id: string;

  datePaiement: string;

  montant: number;

  statut: string;

  modePaiement: string;

  numeroQuittance: string;

  uniteReference: string;

  bienNom: string;

  bienReference: string;
}


export interface HistoriqueLocataireDto {

  locataire: {

    id: string;

    prenom: string;

    nom: string;

    email?: string;

    telephone?: string;

  };

  totalVersements: number;

  nombrePaiements: number;

  historique: HistoriquePaiementDto[];
}


// ============================================================
// RAPPORT - CONTRATS
// ============================================================

export interface RapportContratDto {

  id: string;

  uniteReference: string;

  bienNom: string;

  bienReference: string;

  bienAdresse: string;

  locataireNom: string;

  dateDebut: string;

  dateFin: string;

  montantLoyer: number;

  montantCaution: number;

  statutContrat: string;

  estExpire: boolean;

  joursAvantExpiration: number;

  delaiJoursTolerance: number;
}


// ============================================================
// RAPPORT - IMPAYÉS
// ============================================================

export interface RapportImpayesResponseDto {

  nombreImpayes: number;

  montantTotalImpayes: number;

  impayes: ImpayeDto[];
}


// ============================================================
// NAVIGATION
// ============================================================

export interface NavItem {

  label: string;

  icon: string;

  route: string;
}