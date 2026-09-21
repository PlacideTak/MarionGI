import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule, NgTemplateOutlet } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { ButtonModule } from 'primeng/button';
import { MenuModule } from 'primeng/menu';
import { AvatarModule } from 'primeng/avatar';
import { DividerModule } from 'primeng/divider';
import { DrawerModule } from 'primeng/drawer';
import type { MenuItem } from 'primeng/api';

import { AuthService } from '../login/auth.service';
import { APP_NAV_ITEMS } from '../config/navigation.config';
import { ROLES } from '../models/gestimmo.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    NgTemplateOutlet,
    RouterLink,
    RouterLinkActive,
    ButtonModule,
    MenuModule,
    AvatarModule,
    DividerModule,
    DrawerModule,
    RouterOutlet
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  // Navigation filtrée dynamiquement : les éléments avec des rôles requis 
readonly navItems = computed(() => {
  return APP_NAV_ITEMS.filter(item => {
    if (!item.roles || item.roles.length === 0) {
      return true; // Laisse passer "Contrats"
    }
    return this.authService.hasRole(item.roles);
  });
});

  // Utilisateur connecté réactif
  readonly currentUser = computed(() => {
    const user = this.authService.currentUser();
    return {
      name: user ? `${user.prenom} ${user.nom}`.trim() || user.email : 'Utilisateur',
      role: user ? user.role : '',
    };
  });

  readonly userInitials = computed(() => {
    const name = this.currentUser().name.trim();
    if (!name) return 'U';
    const parts = name.split(' ');
    return parts.length > 1
      ? `${parts[0].charAt(0)}${parts[1].charAt(0)}`.toUpperCase()
      : name.charAt(0).toUpperCase();
  });

  // Vérifie si l'utilisateur est un admin
  readonly isAdmin = computed(() => {
    return this.authService.hasRole([ROLES.Administrateur, ROLES.Admin]);
  });

  // Menu utilisateur dynamique réactif aux rôles
  readonly userMenuItems = computed<MenuItem[]>(() => {
    const items: MenuItem[] = [
      { label: 'Mon profil', icon: 'pi pi-user', command: () => this.router.navigate(['/profil']) }
    ];

    // Ajout conditionnel de l'élément Paramètres
    if (this.isAdmin()) {
      items.push({ label: 'Paramètres', icon: 'pi pi-cog', command: () => this.router.navigate(['/parametres']) });
    }

    items.push(
      { separator: true },
      {
        label: 'Déconnexion',
        icon: 'pi pi-sign-out',
        command: () => this.logout(),
      }
    );

    return items;
  });

  readonly mobileSidebarVisible = signal(false);

  toggleMobileSidebar(): void {
    this.mobileSidebarVisible.update((v) => !v);
  }

  closeMobileSidebar(): void {
    this.mobileSidebarVisible.set(false);
  }

  private logout(): void {
    this.authService.logout();
    this.router.navigate(['/login'], { replaceUrl: true });
  }

  readonly canViewDashboardHome = computed(() => {
    return this.authService.hasRole([ROLES.Administrateur, ROLES.Admin, ROLES.Gestionnaire]);
  });

}