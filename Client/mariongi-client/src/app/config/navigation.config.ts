import { NavItem } from '../dashboard/dashboard.models';

export const APP_NAV_ITEMS: NavItem[] = [
  { label: 'Tableau de bord', icon: 'pi pi-home', route: 'home' },
  { label: 'Biens immobiliers', icon: 'pi pi-building', route: 'biens' },
  { label: 'Contrats', icon: 'pi pi-file', route: 'contrats', roles: ['Administrateur', 'Admin', 'Gestionnaire','Locataire'] },
  { label: 'Demandes de visite', icon: 'pi pi-building-columns', route: 'demandesvisite', roles: ['Administrateur', 'Admin', 'Gestionnaire','Agent'] },
  { label: 'Paiements', icon: 'pi pi-dollar', route: 'paiements',roles: ['Administrateur', 'Admin', 'Gestionnaire'] },
  { label: 'Rapports', icon: 'pi pi-chart-bar', route: 'rapports', roles: ['Administrateur', 'Admin', 'Gestionnaire'] },
  { 
    label: 'Utilisateurs & rôles', 
    icon: 'pi pi-users', 
    route: 'utilisateurs',
    roles: ['Administrateur', 'Admin'] 
  },
];