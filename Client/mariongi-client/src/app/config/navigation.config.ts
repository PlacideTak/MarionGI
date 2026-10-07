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