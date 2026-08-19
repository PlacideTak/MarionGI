import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { BienDetails } from './biendetails/biendetails';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./login/login').then(m => m.Login) },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      { 
        path: '', 
        loadComponent: () => import('./dashboard/dashboard').then(m => m.Dashboard),
        children: [
          // Route par défaut du dashboard (les KPIs et graphiques)
          { 
            path: 'home', 
            loadComponent: () => import('./dashboard/dashboard-home/dashboard-home').then(m => m.DashboardHome) 
          },
          // Page de gestion des utilisateurs
          { 
            path: 'utilisateurs', 
            loadComponent: () => import('./utilisateurs/utilisateurs').then(m => m.Utilisateurs) 
          },
          // Page de gestion des biens
          { 
            path: 'biens', 
            loadComponent: () => import('./biens/biens').then(m => m.Biens) 
          },
          //Contrats
          { 
            path: 'contrats', 
            loadComponent: () => import('./contrats/contrats').then(m => m.Contrats) 
          },
           //paiements
          { 
            path: 'paiements', 
            loadComponent: () => import('./paiements/paiements').then(m => m.Paiements) 
          },
          //paramètres
          { 
            path: 'parametres', 
            loadComponent: () => import('./parametres/parametres').then(m => m.Parametres) 
          },
           //demandes de visite
          { 
            path: 'demandesvisite', 
            loadComponent: () => import('./demandesvisite/demandesvisite').then(m => m.DemandesVisite) 
          },
           //rapports
          { 
            path: 'rapports', 
            loadComponent: () => import('./rapports/rapports').then(m => m.Rapports) 
          },
          { 
            path: 'biens/fiche/:id', 
            component: BienDetails, 
            canActivate: [authGuard] // selon ta garde d'authentification
          },
          // Page de mon profil
          { 
            path: 'profil', 
            loadComponent: () => import('./profil/profil').then(m => m.Profil) 
          },
          { path: '', redirectTo: 'home', pathMatch: 'full' }
        ]
      }
    ]
  },
  { path: '**', redirectTo: 'login' }
];