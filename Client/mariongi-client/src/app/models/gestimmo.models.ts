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
// STATUT DE DÉDUCTIBILITÉ FISCALE
// ============================================================

export enum StatutDeductibilite {
  AQualifier = 0,
  Deductible = 1,
  NonDeductible = 2,
  PartiellementDeductible = 3
}
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

export enum TypePaiement {
  Loyer = 1,
  ChargesLocatives = 2,
  Caution = 3,
  Autre = 4
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
  montantChargesLocatives?: number | null;
  montantCaution?: number | null;
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

  // Identification
  reference: string;

  // Relations
  uniteLocativeId: string;
  locataireId: string;

  uniteLocative?: UniteLocativeDto | null;
  bienImmobilier?: BienDto | null;
  locataire?: UtilisateurDto | null;

  // Période contractuelle
  dateDebut: string;
  dateFin: string;
  dateSignature?: string | null;

  // Conditions financières
  montantLoyer: number;
  montantCaution: number;
  montantChargesLocatives?: number | null;

  frequencePaiement: number;
  delaiJoursTolerance: number;

  // Révision du loyer
  revisionLoyerAutorisee: boolean;
  dateProchaineRevisionLoyer?: string | null;
  tauxRevisionLoyer?: number | null;

  // Renouvellement
  renouvellementAutomatique: boolean;
  delaiPreavisJours?: number | null;

  // Informations documentaires
  cheminDocumentContrat?: string | null;
  observations?: string | null;

  // État
  statut: StatutContrat;
}

export interface CreerContratRequest {
  // Identification
  reference: string;

  // Relations
  uniteLocativeId: string;
  locataireId: string;

  // Période contractuelle
  dateDebut: string;
  dateFin: string;
  dateSignature?: string | null;

  // Conditions financières
  montantLoyer: number;
  montantCaution: number;
  montantChargesLocatives?: number | null;

  frequencePaiement: number;
  delaiJoursTolerance: number;

  // Révision du loyer
  revisionLoyerAutorisee: boolean;
  dateProchaineRevisionLoyer?: string | null;
  tauxRevisionLoyer?: number | null;

  // Renouvellement
  renouvellementAutomatique: boolean;
  delaiPreavisJours?: number | null;

  // Informations complémentaires
  observations?: string | null;
}

export interface ModifierContratRequest {
  // Période contractuelle
  dateDebut: string;
  dateFin: string;
  dateSignature?: string | null;

  // Conditions financières
  montantLoyer: number;
  montantCaution: number;
  montantChargesLocatives?: number | null;

  frequencePaiement: number;
  delaiJoursTolerance: number;

  // Révision du loyer
  revisionLoyerAutorisee: boolean;
  dateProchaineRevisionLoyer?: string | null;
  tauxRevisionLoyer?: number | null;

  // Renouvellement
  renouvellementAutomatique: boolean;
  delaiPreavisJours?: number | null;

  // Informations complémentaires
  observations?: string | null;

  // État
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

  /**
   * Part du paiement correspondant au loyer.
   * Peut être null pour les anciens paiements
   * qui n'ont pas encore été ventilés.
   */
  montantLoyerEncaisse?: number | null;

  /**
   * Part du paiement correspondant aux charges locatives.
   * Peut être null pour les anciens paiements
   * qui n'ont pas encore été ventilés.
   */
  montantChargesLocativesEncaisse?: number | null;
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
  montantLoyerEncaisse?: number | null;
  montantChargesLocativesEncaisse?: number | null;
  typePaiement: TypePaiement;
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

  // Unité locative
  uniteLocativeId: string;
  uniteLocative?: UniteLocativeDto | null;

  // Bien immobilier (informations complémentaires)
  bienId?: string;
  bien?: BienDto | null;

  // Agent affecté
  agentId?: string | null;
  agent?: UtilisateurDto | null;

  // Coordonnées du prospect
  nomProspect: string;
  telephoneProspect: string;
  emailProspect?: string | null;

  // Dates de visite
  dateSouhaitee: string | Date;
  dateConfirmee?: string | Date | null;

  // Commentaires
  observations?: string | null;
  commentaireInterne?: string | null;

  // Suivi
  dateTraitement?: string | Date | null;

  // Statut
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
  reference: string;
  resultatReparation?: string | null;

  bienImmobilierId: string;
  bienImmobilier?: BienDto | null;

  uniteLocativeId?: string | null;
  uniteLocative?: UniteLocativeDto | null;

  locataireId?: string | null;
  locataire?: UtilisateurDto | null;

  declarantId?: string | null;

  dateIncident: string;
  categorie: CategorieIncident;
  description: string;
  priorite: PrioriteIncident;
  statut: StatutIncident;

  dateResolution?: string | null;

  coutEstime?: number | null;
  coutTotal: number;

  imputableA: ImputabiliteIncident;
  validationDevisRequise: boolean;
  devisValide: boolean;
  dateValidationDevis?: string | null;
  valideParUtilisateurId?: string | null;

  photos?: TicketPhotoDto[] | null;
  intervenants?: TicketIntervenantDto[] | null;
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
prestataireId: string;
montantDevis: number | null;
referenceDevis: string | null;
montantFacture: number | null;
referenceFacture: string | null;
dateAssignation: string | null;
dateIntervention: string | null;
dateFinIntervention: string | null;
observations: string | null;
}

export interface ModifierTicketIntervenantRequest {
prestataireId: string;
montantDevis: number | null;
referenceDevis: string | null;
montantFacture: number | null;
referenceFacture: string | null;
dateAssignation: string | null;
dateIntervention: string | null;
dateFinIntervention: string | null;
observations: string | null;
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

  // Traçabilité et validité fiscale
  referenceLegale: string | null;
  dateDebutValidite: string;
  dateFinValidite: string | null;
  dateDerniereVerification: string | null;
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

  referenceLegale: string | null;
  dateDebutValidite: string;
  dateFinValidite: string | null;
  dateDerniereVerification: string | null;
}

/**
 * Requête de modification d'une tranche IRPP.
 */
export interface ModifierTrancheIRPPRequest
  extends CreerTrancheIRPPRequest {
}



// ============================================================
// CHARGES DES BIENS IMMOBILIERS
// ============================================================

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

export const STATUTS_DEDUCTIBILITE_OPTIONS = [
  { label: 'À qualifier', value: StatutDeductibilite.AQualifier },
  { label: 'Déductible', value: StatutDeductibilite.Deductible },
  { label: 'Non déductible', value: StatutDeductibilite.NonDeductible },
  {
    label: 'Partiellement déductible',
    value: StatutDeductibilite.PartiellementDeductible
  }
];


// ============================================================
// CHARGE BIEN - DTO DE CONSULTATION
// GET /api/ChargesBiens
// GET /api/ChargesBiens/{id}
// ============================================================

export interface ChargeBienDto {
  id: string;

  bienImmobilierId: string;
  nomBienImmobilier: string;

  uniteLocativeId: string | null;
  referenceUnite: string | null;

  dateDepense: string;
  dateFacture: string | null;
  datePaiement: string | null;

  typeCharge: TypeCharge;
  description: string;

  nomFournisseur: string | null;
  numeroContribuableFournisseur: string | null;

  montantTTC: number;
  montantHT: number | null;
  montantTVA: number | null;

  referenceFacture: string | null;
  cheminJustificatif: string | null;

  // Qualification fiscale
  statutDeductibilite: StatutDeductibilite;
  montantDeductibleFiscalement: number | null;
  justificationNonDeductibilite: string | null;
  dateValidationFiscale: string | null;

  observations: string | null;

  dateCreation: string;
}

export enum ModeDeductionCharges {
  Forfait30Pourcent = 1,
  FraisReels = 2
}

// ============================================================
// OPTION DE DÉDUCTION FISCALE
// ============================================================

/**
 * DTO retourné par OptionsDeductionFiscalesController.
 */
export interface OptionDeductionFiscaleDto {
  id: string;
  societeId: string;
  anneeFiscale: number;
  modeDeduction: ModeDeductionCharges;
  dateConfirmation: string | null;
  estVerrouillee: boolean;
  dateVerrouillage: string | null;
  observations: string | null;
  dateCreation: string;
}

/**
 * Requête de création d'une option de déduction fiscale.
 */
export interface CreerOptionDeductionFiscaleRequest {
  anneeFiscale: number;
  modeDeduction: ModeDeductionCharges;
  observations: string | null;
}

/**
 * Requête de modification d'une option de déduction fiscale.
 */
export interface ModifierOptionDeductionFiscaleRequest
  extends CreerOptionDeductionFiscaleRequest {
}

/**
 * Options destinées au composant de sélection PrimeNG.
 */
export const MODES_DEDUCTION_OPTIONS = [
  {
    label: 'Forfait de 30 %',
    value: ModeDeductionCharges.Forfait30Pourcent
  },
  {
    label: 'Frais réels',
    value: ModeDeductionCharges.FraisReels
  }
];

// ============================================================
// CHARGE BIEN - REQUÊTE DE CRÉATION
// POST /api/ChargesBiens
// ============================================================

export interface CreerChargeBienRequest {
  bienImmobilierId: string;
  uniteLocativeId: string | null;

  dateDepense: string;
  dateFacture: string | null;
  datePaiement: string | null;

  typeCharge: TypeCharge;
  description: string;

  nomFournisseur: string | null;
  numeroContribuableFournisseur: string | null;

  montantTTC: number;
  montantHT: number | null;
  montantTVA: number | null;

  referenceFacture: string | null;
  cheminJustificatif: string | null;

  observations: string | null;
}


// ============================================================
// CHARGE BIEN - REQUÊTE DE MODIFICATION
// PUT /api/ChargesBiens/{id}
// ============================================================

export interface ModifierChargeBienRequest
  extends CreerChargeBienRequest {}


// ============================================================
// RÉPONSE DES OPÉRATIONS SUR UNE CHARGE
// POST / PUT / DELETE
// ============================================================

export interface ChargeBienOperationResponse {
  message: string;

  id?: string;

  bienImmobilierId?: string;
  uniteLocativeId?: string | null;

  dateDepense?: string;
  typeCharge?: TypeCharge;
  montantTTC?: number;

  statutDeductibilite?: StatutDeductibilite;
  montantDeductibleFiscalement?: number | null;
}

// ============================================================
// SUPPRESSION D'UNE CHARGE
// DELETE /api/ChargesBiens/{id}
// ============================================================

export interface SupprimerChargeBienResponse {
  message: string;
}

// ============================================================
// FILTRES DE RECHERCHE DES CHARGES
// ============================================================

export interface FiltreChargesBiens {
  bienImmobilierId?: string | null;
  uniteLocativeId?: string | null;
  typeCharge?: TypeCharge | null;
  dateDebut?: string | null;
  dateFin?: string | null;
}

export interface PrestataireDto {
  id: string;
  typePrestataire: TypePersonne;
  nom: string;
  prenom: string;
  nomEntreprise?: string | null;
  specialite: string;
  telephone: string;
  courriel?: string | null;
  adresse?: string | null;
  ville?: string | null;
  numeroContribuable?: string | null;
  observations?: string | null;
  actif: boolean;
  dateCreation: string;
}

export interface CreerPrestataireRequest {
  typePrestataire: TypePersonne;
  nom: string;
  prenom: string;
  nomEntreprise?: string | null;
  specialite: string;
  telephone: string;
  courriel?: string | null;
  adresse?: string | null;
  ville?: string | null;
  numeroContribuable?: string | null;
  observations?: string | null;
}

export interface ModifierPrestataireRequest
  extends CreerPrestataireRequest {}

export interface ModifierStatutPrestataireRequest {
  actif: boolean;
}

export interface PrestataireOption {
  label: string;
  value: string;
}

export enum TypeValeurFiscale {
  Estimee = 1,
  Declaree = 2,
  Officielle = 3,
  Expertise = 4
}

export interface ValeurFiscaleBienDto {
  id: string;
  bienImmobilierId: string;
  referenceBien: string;
  nomBien: string;
  anneeFiscale: number;
  montant: number;
  typeValeur: TypeValeurFiscale;
  dateEvaluation: string;
  dateValidationOfficielle: string | null;
  referenceJustificative: string | null;
  cheminJustificatif: string | null;
  observations: string | null;
  dateCreation: string;
}

export interface CreerValeurFiscaleBienRequest {
  bienImmobilierId: string;
  anneeFiscale: number;
  montant: number;
  typeValeur: TypeValeurFiscale;
  dateEvaluation: string;
  dateValidationOfficielle: string | null;
  referenceJustificative: string | null;
  cheminJustificatif: string | null;
  observations: string | null;
}

export type ModifierValeurFiscaleBienRequest =
  CreerValeurFiscaleBienRequest;

  export enum TypeImpotFiscal {
  NonDefini = 0,
  ProprieteFonciere = 1,
  RevenusFonciers = 2,
  PrecompteSurLoyers = 3,
  IRPP = 4,
  ImpotSocietes = 5,
  PlusValueImmobiliere = 6,
  DroitsEnregistrement = 7,
  TVA = 8,
  Autre = 99
}

export enum StatutDeclarationFiscale {
  APreparer = 1,
  ADeclarer = 2,
  Declaree = 3,
  ARegler = 4,
  PartiellementReglee = 5,
  Reglee = 6,
  EnRetard = 7
}

export interface BienDeclarationFiscaleDto {
  id: string;
  reference: string;
  nom: string;
  adresse?: string | null;
  ville?: string | null;
  quartier?: string | null;
}

export interface DeclarationFiscaleDto {
  id: string;
  societeId: string;

  bienImmobilierId: string | null;
  bienImmobilier: BienDeclarationFiscaleDto | null;

  anneeFiscale: number;
  typeImpot: TypeImpotFiscal;

  baseImposable: number | null;
  montantImpot: number;

  dateEcheance: string | null;
  dateDeclaration: string | null;

  referenceDeclaration: string | null;
  observations: string | null;

  statut: StatutDeclarationFiscale;

  // Option de déduction
  optionDeductionFiscaleId: string | null;
  modeDeduction: ModeDeductionCharges | null;

  // Résultats du calcul fiscal
  totalLoyersBruts: number | null;
  chargesForfaitaires: number | null;
  chargesReellesDeclarees: number | null;
  chargesReellesAdmissibles: number | null;
  revenuNetFoncier: number | null;

  acomptesVerses: number | null;
  soldeImpot: number | null;

  dateCalcul: string | null;

  dateCreation: string;
}

export interface DeclarationFiscaleRequest {
  id?: string;

  bienImmobilierId: string | null;
  anneeFiscale: number;
  typeImpot: TypeImpotFiscal;

  baseImposable: number | null;
  montantImpot: number;

  dateEcheance: string | null;
  dateDeclaration: string | null;

  referenceDeclaration: string | null;
  observations: string | null;

  statut: StatutDeclarationFiscale;

  // Option de déduction fiscale
  optionDeductionFiscaleId: string | null;
}

// ============================================================
// DECLARATION CHARGE BIEN
// ============================================================

export interface DeclarationChargeBienDto {
  id: string;
  declarationFiscaleId: string;
  chargeBienId: string;
  descriptionCharge: string;
  dateDepense: string;
  montantTTC: number;
  statutDeductibilite: StatutDeductibilite;

  /**
   * Montant fiscalement admissible au moment
   * de l'association de la charge à la déclaration.
   */
  montantDeductibleFiscalement: number | null;

  /**
   * Montant effectivement retenu dans la déclaration.
   */
  montantRetenu: number;

  dateCreation?: string;
}

export interface AjouterDeclarationChargeBienRequest {
  declarationFiscaleId: string;
  chargeBienId: string;
  montantRetenu: number;
}

export interface ModifierDeclarationChargeBienRequest {
  montantRetenu: number;
}

// ============================================================
// AJOUT D'UNE CHARGE À UNE DÉCLARATION
// POST /api/DeclarationChargeBiens
// ============================================================

export interface AjouterDeclarationChargeBienRequest {
  declarationFiscaleId: string;
  chargeBienId: string;
  montantRetenu: number;
}

// ============================================================
// MODIFICATION D'UNE CHARGE RETENUE
// PUT /api/DeclarationChargeBiens/{id}
// ============================================================

export interface ModifierDeclarationChargeBienRequest {
  montantRetenu: number;
}

// ============================================================
// SUPPRESSION D'UNE CHARGE RETENUE
// DELETE /api/DeclarationChargeBiens/{id}
// ============================================================

export interface DeclarationChargeBienOperationResponse {
  message: string;
  id?: string;
}

export interface CalculFiscalResponse {
  message: string;
  id: string;
  anneeFiscale: number;
  modeDeduction: number | null;

  totalLoyersBruts: number | null;
  chargesForfaitaires: number | null;
  chargesReellesDeclarees: number | null;
  chargesReellesAdmissibles: number | null;

  revenuNetFoncier: number | null;
  baseImposable: number | null;

  montantImpotPrincipal: number | null;
  tauxCac: number | null;
  montantCac: number | null;

  montantImpot: number;
  acomptesVerses: number | null;
  soldeImpot: number | null;

  dateCalcul: string | null;
}
