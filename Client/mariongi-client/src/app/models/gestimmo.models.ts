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


// ============================================================
// UTILISATEUR
// ============================================================

/**
 * Réponse de l'API : UtilisateurResponse.
 *
 * Ce modèle représente un utilisateur réellement retourné
 * par l'API ASP.NET Core.
 */
export interface UtilisateurDto extends BaseEntity {
  nom: string;
  prenom: string;
  telephone: string;
  email: string;
  role: RoleUtilisateur;
  statut: boolean;

  societeId: string;
  nomSociete: string;

  derniereConnexion: string | null;
  telephoneVerifie: boolean;
  dateDerniereModificationProfil: string | null;
}

/**
 * Modèle local utilisé pour initialiser un formulaire
 * de création d'utilisateur.
 *
 * Il ne représente pas encore un utilisateur enregistré.
 */
export interface NouvelUtilisateurForm {
  id: string;
  nom: string;
  prenom: string;
  email: string;
  telephone: string;
  role: RoleUtilisateur;
  statut: boolean;
  societeId: string;
}

/**
 * Requête de création : CreerUtilisateurRequest.
 *
 * Correspond exactement au contrat de l'API C#.
 */
export interface CreerUtilisateurRequest {
  nom: string;
  prenom: string;
  telephone: string | null;
  email: string;
  motDePasse: string;
  role: RoleUtilisateur;
  societeId: string;
}

/**
 * Requête de modification : ModifierUtilisateurRequest.
 *
 * Le statut et le mot de passe ne sont pas modifiés
 * par cet endpoint PUT.
 */
export interface ModifierUtilisateurRequest {
  nom: string;
  prenom: string;
  telephone: string | null;
  email: string;
  role: RoleUtilisateur;
  societeId: string;
}

/**
 * Requête de modification du statut :
 * ModifierStatutUtilisateurRequest.
 */
export interface ModifierStatutUtilisateurRequest {
  statut: boolean;
}

// ============================================================
// BIEN IMMOBILIER
// ============================================================


export interface BienDto extends BaseEntity {
  reference: string;
  nom: string;
  type: TypeBien;

  adresse: string;
  ville: string;
  quartier?: string | null;
  region?: string | null;
  departement?: string | null;
  arrondissement?: string | null;

  superficie: number;
  anneeConstruction?: number | null;
  nombreEtages?: number | null;

  numeroTitreFoncier?: string | null;
  numeroLot?: string | null;
  numeroParcelle?: string | null;
  referenceCadastrale?: string | null;

  valeurAcquisition?: number | null;
  dateAcquisition?: string | null;
  valeurEstimee?: number | null;
  dateDerniereEvaluation?: string | null;

  // À typer avec l'enum C# exact quand il sera disponible.
  statutFoncier?: number | null;

  photos?: string[];

  societeId: string;
  societeNom?: string;
  nombreUnites?: number;
}

export enum StatutFoncier {
  NonRenseigne = 0,
  TitreFoncierDisponible = 1,
  DemandeEnCours = 2,
  SansTitreFoncier = 3,
  Autre = 99
}

export interface BienDetailDto extends BienDto {
  unitesLocatives: UniteLocativeDto[];
}

export interface CreerBienRequest {
  reference: string;
  nom: string;
  type: TypeBien;

  adresse: string;
  ville: string;
  quartier?: string | null;
  region?: string | null;
  departement?: string | null;
  arrondissement?: string | null;

  superficie: number;
  anneeConstruction?: number | null;
  nombreEtages?: number | null;

  numeroTitreFoncier?: string | null;
  numeroLot?: string | null;
  numeroParcelle?: string | null;
  referenceCadastrale?: string | null;

  valeurAcquisition?: number | null;
  dateAcquisition?: string | null;
  valeurEstimee?: number | null;
  dateDerniereEvaluation?: string | null;
  statutFoncier?: number | null;

  fichiers?: File[];
}

export interface ModifierBienRequest extends CreerBienRequest {
  photosExistantes?: string[];
}



 // ============================================================
 // UNITÉ LOCATIVE
 // ============================================================

/**
 * Réponse de l'API représentant une unité locative.
 * Les champs complémentaires liés au bien, aux contrats et aux
 * statistiques peuvent dépendre de l'endpoint appelé.
 */
export interface UniteLocativeDto extends BaseEntity {
  reference: string;
  type: TypeUniteLocative;
  superficie: number;
  loyer: number;
  statut: StatutBien;
  bienImmobilierId: string;
  photos: string[];

  // Informations complémentaires de l'unité
  nombrePieces?: number | null;
  etage?: number | null;
  chargesLocativesMensuelles?: number | null;
  montantCaution?: number | null;
  datePremiereMiseEnLocation?: string | null;
  dateDerniereRenovation?: string | null;
  estExonereeFiscalement?: boolean;
  motifExoneration?: string | null;

  // Informations du bien parent
  bienReference?: string;
  bienNom?: string;
  bienAdresse?: string;
  bienVille?: string;
  bienQuartier?: string | null;

  // Informations du propriétaire / compte
  societeId?: string;
  societeNom?: string;

  // Contrats associés
  contrats?: ContratUniteDto[];
  aContratActif?: boolean;

  // Statistiques
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
    nom: string;
    adresse: string;
    ville: string;
    quartier?: string | null;
    societeId: string;
  };

  contrats?: ContratUniteDto[];
  nombreDemandesVisite?: number;
}


// ============================================================
// CONTRAT ASSOCIÉ À UNE UNITÉ LOCATIVE
// ============================================================

export interface ContratUniteDto {
  id: string;
  reference: string;
  statut: StatutContrat;
  dateDebut: string;
  dateFin?: string | null;
  montantLoyer: number;
}


// ============================================================
// CHAMPS COMMUNS AUX REQUÊTES D'UNITÉ LOCATIVE
// ============================================================

/**
 * Champs modifiables d'une unité.
 * Les propriétés facultatives correspondent aux types nullable
 * du DTO C#.
 */
export interface UniteLocativeRequestBase {
  reference: string;
  type: TypeUniteLocative;
  superficie: number;
  nombrePieces: number | null;
  etage: number | null;
  loyer: number;
  chargesLocativesMensuelles: number | null;
  montantCaution: number | null;
  statut: StatutBien;
  datePremiereMiseEnLocation: string | null;
  dateDerniereRenovation: string | null;
  estExonereeFiscalement: boolean;
  motifExoneration: string | null;
  bienImmobilierId: string;
}


// ============================================================
// CRÉATION D'UNE UNITÉ LOCATIVE
// ============================================================

/**
 * Correspond aux champs métier de la création.
 * Les fichiers sont envoyés séparément via FormData.
 */
export interface CreerUniteLocativeRequest
  extends UniteLocativeRequestBase {
  fichiers?: File[];
}


// ============================================================
// MODIFICATION D'UNE UNITÉ LOCATIVE
// ============================================================

/**
 * Correspond à ModifierUniteLocativeRequest côté C#.
 *
 * photosExistantes permet de préciser les photos conservées.
 * fichiers contient les nouveaux fichiers à téléverser.
 */
export interface ModifierUniteLocativeRequest
  extends UniteLocativeRequestBase {
  photosExistantes?: string[] | null;
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
  // UNITÉ LOCATIVE
  // ----------------------------------------------------------

  uniteLocative?: UniteLocativeDto | null;


  // ----------------------------------------------------------
  // BIEN IMMOBILIER
  // ----------------------------------------------------------

  /**
   * Bien immobilier associé à l'unité locative.
   *
   * Retourne par l'API sous la forme :
   *
   * "bienImmobilier": {
   *   "id": "...",
   *   "reference": "...",
   *   "nom": "...",
   *   "adresse": "...",
   *   "ville": "...",
   *   "quartier": "..."
   * }
   */
  bienImmobilier?: BienDto | null;


  // ----------------------------------------------------------
  // LOCATAIRE
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

  /**
   * Premier jour du mois payé.
   *
   * Exemple :
   * 2026-10-01 = loyer d'octobre 2026
   */
  moisLoyer: string;
}


// ============================================================
// LISTE DES PAIEMENTS
// ============================================================

export interface PaiementListItem {
  id: string;
  contratId: string;
  datePaiement: string;
  moisLoyer: string;
  montant: number;
  mode: ModePaiement;
  statut: StatutTransaction;
  numeroQuittance: string;
  locataireNom: string;
  uniteReference: string;
  bienReference: string;
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


// ============================================================
// PROPRIÉTAIRE : PERSONNE PHYSIQUE OU PERSONNE MORALE
// ============================================================

/**
 * Les valeurs numériques doivent correspondre aux enums C#.
 */
export enum TypePersonne {
  PersonnePhysique = 1,
  PersonneMorale = 2
}

export enum FormeJuridique {
  NonApplicable = 0,
  EntrepriseIndividuelle = 1,
  SARL = 2,
  SA = 3,
  SAS = 4,
  SNC = 5,
  SCS = 6,
  GIE = 7,
  Autre = 99
}

/**
 * À aligner sur les valeurs exactes de l'enum FormeJuridique
 * défini dans MarionGI.Domain.Enums.
 */
// Déclarer ici FormeJuridique après vérification de l'enum C#.


// ============================================================
// SOCIÉTÉ / PROPRIÉTAIRE - DTO DE RÉPONSE
// ============================================================

export interface SocieteDto extends BaseEntity {
  typePersonne: TypePersonne;

  nom: string;
  prenom?: string | null;

  formeJuridique?: FormeJuridique | null;

  numeroEntreprise?: string | null;
  numeroContribuable?: string | null;

  adresse?: string | null;
  adresseFiscale?: string | null;

  ville?: string | null;
  region?: string | null;
  departement?: string | null;
  codePostal?: string | null;
  pays?: string | null;

  centreImpots?: string | null;
  activitePrincipale?: string | null;

  telephone?: string | null;
  email?: string | null;

  actif: boolean;

  nombreUtilisateurs: number;
  nombreBiens: number;
  nombreDeclarationsFiscales: number;
}


// ============================================================
// CRÉATION D'UN PROPRIÉTAIRE
// ============================================================

export interface CreerSocieteRequest {
  typePersonne: TypePersonne;

  nom: string;
  prenom?: string | null;

  formeJuridique?: FormeJuridique | null;

  numeroEntreprise?: string | null;
  numeroContribuable?: string | null;

  adresse?: string | null;
  adresseFiscale?: string | null;

  ville?: string | null;
  region?: string | null;
  departement?: string | null;
  codePostal?: string | null;
  pays?: string | null;

  centreImpots?: string | null;
  activitePrincipale?: string | null;

  telephone?: string | null;
  email?: string | null;

  actif: boolean;
}


// ============================================================
// MODIFICATION D'UN PROPRIÉTAIRE
// ============================================================

export interface ModifierSocieteRequest {
  typePersonne: TypePersonne;

  nom: string;
  prenom?: string | null;

  formeJuridique?: number | null;

  numeroEntreprise?: string | null;
  numeroContribuable?: string | null;

  adresse?: string | null;
  adresseFiscale?: string | null;

  ville?: string | null;
  region?: string | null;
  departement?: string | null;
  codePostal?: string | null;
  pays?: string | null;

  centreImpots?: string | null;
  activitePrincipale?: string | null;

  telephone?: string | null;
  email?: string | null;
}


// ============================================================
// MODIFICATION DU STATUT
// ============================================================

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


// ============================================================
// TICKETS D'INCIDENTS
// ============================================================

export enum CategorieIncident {
  Plomberie = 1,
  Electricite = 2,
  Maconnerie = 3,
  Securite = 4,
  Autre = 5
}

export enum PrioriteIncident {
  Basse = 1,
  Moyenne = 2,
  Haute = 3,
  Urgente = 4
}

export enum StatutIncident {
  Ouvert = 1,
  EnCours = 2,
  EnAttenteValidation = 3,
  Resolu = 4,
  Cloture = 5
}

export enum ImputabiliteIncident {
  Indeterminee = 0,
  Societe = 1,
  Locataire = 2
}

export enum TypePhotoIncident {
  AvantIntervention = 1,
  ApresIntervention = 2
}


// ============================================================
// INTERVENANT D'UN TICKET
// ============================================================

export interface TicketIntervenantDto extends BaseEntity {
  ticketIncidentId: string;
  nomArtisan: string;
  specialite: string;
  telephone: string;
  courriel?: string | null;
  montantDevis?: number | null;
  montantFacture?: number | null;
  dateAssignation?: string | null;
  dateIntervention?: string | null;
}


// ============================================================
// PHOTO D'UN TICKET
// ============================================================

export interface TicketPhotoDto extends BaseEntity {
  ticketIncidentId: string;
  urlPhoto: string;
  type: TypePhotoIncident;
  ajouteParUtilisateurId?: string | null;
  dateAjout: string;
}


// ============================================================
// TICKET - ÉLÉMENT DE LISTE
// ============================================================

export interface LocataireOption {
  id: string;
  nom: string;
  prenom: string;
}

export interface TicketIncidentDto extends BaseEntity {
  uniteLocativeId: string;
  locataireId: string;
  reference?: string;
  categorie: CategorieIncident;
  description: string;
  dateincident?: string;

  priorite: PrioriteIncident;
  statut: StatutIncident;

  dateResolution?: string | null;
  resultatReparation?: string | null;

  coutEstime?: number | null;
  coutTotal: number;

  imputableA: ImputabiliteIncident;
  validationDevisRequise: boolean;
  devisValide: boolean;
  dateValidationDevis?: string | null;
  valideParUtilisateurId?: string | null;

  // Informations complémentaires renvoyées par l'API
  locataireNom?: string;
  locataireTelephone?: string;

  uniteReference?: string;
  uniteType?: TypeUniteLocative;

  bienImmobilierId?: string;
  bienReference?: string;
  bienNom?: string;
  bienAdresse?: string;
  bienVille?: string;

  societeId?: string;
  societeNom?: string;

  nombrePhotos?: number;
  nombreIntervenants?: number;
}


// ============================================================
// TICKET - DÉTAIL
// ============================================================

export interface TicketIncidentDetailDto extends TicketIncidentDto {
  photos?: TicketPhotoDto[] | null;
  intervenants: TicketIntervenantDto[];
}


// ============================================================
// CRÉATION D'UN TICKET
// ============================================================

export interface CreerTicketIncidentRequest {
  uniteLocativeId: string;
  locataireId?: string;

  categorie: CategorieIncident;
  description: string;
  descriptionDegats?: string | null;

  priorite: PrioriteIncident;

  fichiers?: File[];
}


// ============================================================
// MODIFICATION D'UN TICKET
// ============================================================

export interface ModifierTicketIncidentRequest {
  categorie: CategorieIncident;
  description: string;
  descriptionDegats?: string | null;

  priorite: PrioriteIncident;

  coutEstime?: number | null;
  imputableA: ImputabiliteIncident;

  validationDevisRequise: boolean;
}


// ============================================================
// CHANGEMENT DE STATUT
// ============================================================

export interface ModifierStatutTicketRequest {
  statut: StatutIncident;
  resultatReparation?: string | null;
}


// ============================================================
// VALIDATION D'UN DEVIS
// ============================================================

export interface ValiderDevisTicketRequest {
  devisValide: boolean;
}


// ============================================================
// AJOUT D'UN INTERVENANT
// ============================================================

export interface CreerTicketIntervenantRequest {
  nomArtisan: string;
  specialite: string;
  telephone: string;
  courriel: string;

  montantDevis?: number | null;
  montantFacture?: number | null;

  dateAssignation?: string | null;
  dateIntervention?: string | null;
}

export interface ModifierTicketIntervenantRequest {
  nomArtisan: string;
  specialite: string;
  telephone: string;
  courriel?: string | null;
  montantDevis?: number | null;
  montantFacture?: number | null;
  dateIntervention?: string | null;
}

// ============================================================
// FILTRES DE RECHERCHE
// ============================================================

export interface TicketIncidentFilterCriteria {
  recherche?: string;

  statut?: StatutIncident;
  priorite?: PrioriteIncident;
  categorie?: CategorieIncident;

  uniteLocativeId?: string;

  dateDebut?: string;
  dateFin?: string;
}

// DTO correspondant aux données retournées par l'API.
export interface RegimeFiscalDto {
  id: string;
  code: string;
  libelle: string;
  description: string;
  estActif: boolean;
  dateCreation: string;
}

// DTO utilisé pour créer un régime fiscal.
export interface CreerRegimeFiscalRequest  {
  code: string;
  libelle: string;
  description: string;
  estActif: boolean;
}

// DTO utilisé pour modifier un régime fiscal.
export interface ModifierRegimeFiscalRequest
  extends CreerRegimeFiscalRequest {}


// ============================================================
// PARAMÈTRE FISCAL - DTO DE RÉPONSE
// ============================================================

export interface ParametreFiscalDto {
  id: string;
  anneeFiscale: number;
  codeRegle: string;
  description: string;
  valeur: number;
  typeParametre: number;
  dateDebutValidite: string;
  dateFinValidite: string | null;
  dateCreation: string;
}

// ============================================================
// PARAMÈTRE FISCAL - DTO DE CRÉATION
// ============================================================

export interface CreerParametreFiscalRequest {
  anneeFiscale: number;
  codeRegle: string;
  description: string;
  valeur: number;
  typeParametre: number;
  dateDebutValidite: string;
  dateFinValidite: string | null;
}

// ============================================================
// PARAMÈTRE FISCAL - DTO DE MODIFICATION
// ============================================================

export interface ModifierParametreFiscalRequest
  extends CreerParametreFiscalRequest {
}

// ============================================================
// TRANCHE IRPP
// ============================================================

/**
 * DTO de lecture d'une tranche IRPP.
 */
export interface TrancheIRPPDto {
  id: string;
  anneeFiscale: number;
  seuilMin: number;
  seuilMax: number | null;
  taux: number;
  ordreTranche: number;
  description: string | null;
}

/**
 * Requête de création d'une tranche IRPP.
 */
export interface CreerTrancheIRPPRequest {
  anneeFiscale: number;
  seuilMin: number;
  seuilMax: number | null;
  taux: number;
  ordreTranche: number;
  description: string;
}

/**
 * Requête de modification d'une tranche IRPP.
 */
export interface ModifierTrancheIRPPRequest
  extends CreerTrancheIRPPRequest {
}

export enum TypeCharge {
  TaxeFonciere = 1,
  Entretien = 2,
  Assurance = 3,
  InteretEmprunt = 4
}

export const TYPES_CHARGES_OPTIONS = [
  { label: 'Taxe foncière', value: TypeCharge.TaxeFonciere },
  { label: 'Entretien et réparations', value: TypeCharge.Entretien },
  { label: 'Assurance', value: TypeCharge.Assurance },
  { label: 'Intérêts d’emprunt', value: TypeCharge.InteretEmprunt }
];

// ============================================================
// MODÈLES
// ============================================================

export interface ChargeBienDto {
  id: string;
  bienImmobilierId: string;
  nomBienImmobilier: string;
  uniteLocativeId: string | null;
  referenceUnite: string | null;
  dateDepense: string;
  typeCharge: TypeCharge;
  description: string;
  montantTTC: number;
  montantHT: number | null;
  montantTVA: number | null;
  referenceFacture: string | null;
  estDeductibleFiscalement: boolean;
  justificationNonDeductibilite: string | null;
  dateCreation: string;
}

export interface CreerChargeBienRequest {
  bienImmobilierId: string;
  uniteLocativeId: string | null;
  dateDepense: string;
  typeCharge: TypeCharge;
  description: string;
  montantTTC: number;
  montantHT: number | null;
  montantTVA: number | null;
  referenceFacture: string | null;
  estDeductibleFiscalement: boolean;
  justificationNonDeductibilite: string | null;
}

export interface ModifierChargeBienRequest
  extends CreerChargeBienRequest {}

export interface SupprimerChargeBienResponse {
  message: string;
}

export interface FiltreChargesBiens {
  bienImmobilierId?: string | null;
  uniteLocativeId?: string | null;
  typeCharge?: TypeCharge | null;
  dateDebut?: string | null;
  dateFin?: string | null;
}
