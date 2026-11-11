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
    label: 'Valeurs fiscales',
    icon: 'pi pi-chart-line',
    route: 'valeursfiscalesbiens',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire
    ]
  },
 {
    label: 'Options de déductions fiscales',
    icon: 'pi pi-calculator',
    route: 'optionsdeductionfiscales',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire
    ]
  },
  {
    label: 'Déclarations fiscales',
    icon: 'pi pi-file-edit',
    route: 'declarationsfiscales',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire
    ]
  },
   {
    label: 'Affectation des charges aux déclarations fiscales',
    icon: 'pi pi-calculator',
    route: 'declarationschargesbiens',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire
    ]
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
    label: 'Prestataires',
    icon: 'pi pi-briefcase',
    route: 'prestataires',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire
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
    label: 'Eats des paiements',
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
    label: 'Calcul fiscal',
    icon: 'pi pi-calculator',
    route: 'calculfiscal',
    roles: [
      ROLES.Administrateur,
      ROLES.Admin,
      ROLES.Gestionnaire
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