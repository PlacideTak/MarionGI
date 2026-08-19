import { NavItem } from '../dashboard/dashboard.models';

export const APP_NAV_ITEMS: NavItem[] = [
  { label: 'Tableau de bord', icon: 'pi pi-home', route: '/' },
  { label: 'Biens immobiliers', icon: 'pi pi-building', route: '/biens' },
  { label: 'Contrats', icon: 'pi pi-file', route: '/contrats' },
  { label: 'Demandes de visite', icon: 'pi pi-building-columns', route: '/demandesvisite' },
  { label: 'Paiements', icon: 'pi pi-dollar', route: '/paiements' },
  { label: 'Rapports', icon: 'pi pi-chart-bar', route: '/rapports' },
  { label: 'Utilisateurs & rôles', icon: 'pi pi-users', route: '/utilisateurs' },
];