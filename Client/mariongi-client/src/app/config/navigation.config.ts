import { NavItem } from '../dashboard/dashboard.models';
import { ROLES } from '../models/gestimmo.models';

export const APP_NAV_ITEMS: NavItem[] = [
  {
    label: 'Tableau de bord',
    icon: 'pi pi-home',
    route: 'home'
  },

  {
    label: 'Sociétés',
    icon: 'pi pi-building',
    route: 'societes',
    roles: [ROLES.Administrateur, ROLES.Admin]
  },

  {
    label: 'Biens immobiliers',
    icon: 'pi pi-building',
    route: 'biens',
    roles: [ROLES.Administrateur, ROLES.Admin]
  },

  {
    label: 'Unités locatives',
    icon: 'pi pi-th-large',
    route: 'uniteslocatives',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire,
      ROLES.Agent,
      ROLES.Locataire
    ]
  },

  {
    label: 'Contrats',
    icon: 'pi pi-file',
    route: 'contrats',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire,
      ROLES.Locataire
    ]
  },

  {
    label: 'Demandes de visite',
    icon: 'pi pi-building-columns',
    route: 'demandesvisite',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire,
      ROLES.Agent
    ]
  },

  {
    label: 'Incidents / Tickets',
    icon: 'pi pi-wrench',
    route: 'tickets',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire,
      ROLES.Locataire
    ]
  },

  {
    label: 'Paiements',
    icon: 'pi pi-dollar',
    route: 'paiements',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire,
      ROLES.Locataire
    ]
  },

  // ========================================================
  // PRESTATAIRES ET GESTION IMMOBILIÈRE
  // ========================================================

  {
    label: 'Prestataires',
    icon: 'pi pi-briefcase',
    route: 'prestataires',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire
    ]
  },

  // ========================================================
  // GESTION FISCALE ET CHARGES IMMOBILIÈRES
  // ========================================================

  {
    label: 'Charges immobilières',
    icon: 'pi pi-wallet',
    route: 'chargesimmobilieres',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire
    ]
  },

  {
    label: 'Tranches IRPP',
    icon: 'pi pi-calculator',
    route: 'tranchesirpp',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire
    ]
  },

  {
    label: 'Paramètres fiscaux',
    icon: 'pi pi-sliders-h',
    route: 'parametresfiscaux',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire
    ]
  },

  {
    label: 'Régimes fiscaux',
    icon: 'pi pi-briefcase',
    route: 'regimesfiscaux',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire
    ]
  },

  {
    label: 'Valeurs fiscales des biens',
    icon: 'pi pi-chart-line',
    route: 'valeursfiscalesbiens',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire
    ]
  },

  {
    label: 'Déclarations fiscales des biens',
    icon: 'pi pi-file-edit',
    route: 'declarationsfiscalesbiens',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire
    ]
  },

  // ========================================================
  // RAPPORTS ET ADMINISTRATION
  // ========================================================

  {
    label: 'Rapports',
    icon: 'pi pi-chart-bar',
    route: 'rapports',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire
    ]
  },

  {
    label: 'Utilisateurs & rôles',
    icon: 'pi pi-users',
    route: 'utilisateurs',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin
    ]
  }
];