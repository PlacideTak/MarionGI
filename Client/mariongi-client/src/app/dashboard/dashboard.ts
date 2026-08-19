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

  // Navigation centralisée
  readonly navItems = signal(APP_NAV_ITEMS);

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

  readonly userMenuItems: MenuItem[] = [
    { label: 'Mon profil', icon: 'pi pi-user',command: () => this.router.navigate(['/profil']) },
    { label: 'Paramètres', icon: 'pi pi-cog' ,command: () => this.router.navigate(['/parametres']) },
    { separator: true },
    {
      label: 'Déconnexion',
      icon: 'pi pi-sign-out',
      command: () => this.logout(),
    },
  ];

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
}