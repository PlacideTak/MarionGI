import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { 
    path: 'login', 
    loadComponent: () => import('./login/login').then(m => m.Login) 
  },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      { 
        path: '', 
        loadComponent: () => import('./dashboard/dashboard').then(m => m.Dashboard),
        children: [
          { path: '', redirectTo: 'home', pathMatch: 'full' },
          { 
            path: 'home', 
            loadComponent: () => import('./dashboard/dashboard-home/dashboard-home').then(m => m.DashboardHome) 
          },
          { 
            path: 'utilisateurs', 
            loadComponent: () => import('./utilisateurs/utilisateurs').then(m => m.Utilisateurs) 
          },
          { 
            path: 'biens', 
            loadComponent: () => import('./biens/biens').then(m => m.Biens) 
          },
          { 
            path: 'biens/fiche/:id', 
            loadComponent: () => import('./biendetails/biendetails').then(m => m.BienDetails) 
          },
          { 
            path: 'contrats', 
            loadComponent: () => import('./contrats/contrats').then(m => m.Contrats) 
          },
          { 
            path: 'paiements', 
            loadComponent: () => import('./paiements/paiements').then(m => m.Paiements) 
          },
          { 
            path: 'demandesvisite', 
            loadComponent: () => import('./demandesvisite/demandesvisite').then(m => m.DemandesVisite) 
          },
          { 
            path: 'rapports', 
            loadComponent: () => import('./rapports/rapports').then(m => m.Rapports) 
          },
          { 
            path: 'parametres', 
            loadComponent: () => import('./parametres/parametres').then(m => m.Parametres) 
          },
          { 
            path: 'profil', 
            loadComponent: () => import('./profil/profil').then(m => m.Profil) 
          },
          { 
            path: 'societes', 
            loadComponent: () => import('./societes/societes').then(m => m.Societes) 
          },
           { 
            path: 'uniteslocatives', 
            loadComponent: () => import('./uniteslocatives/uniteslocatives').then(m => m.UnitesLocatives) 
          }
        ]
      }
    ]
  },
  { path: '**', redirectTo: 'login' }
];