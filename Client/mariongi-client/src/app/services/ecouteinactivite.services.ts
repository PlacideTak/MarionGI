import { Injectable, NgZone } from '@angular/core';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class ecouteinactivite {
  private timeoutId: any;
  private readonly IDLE_TIMEOUT = 1 * 60 * 1000; // 1 minute (en millisecondes)

  constructor(private router: Router, private ngZone: NgZone) {}

  public startWatching() {
    this.resetTimer();
    
    // Écoute des événements utilisateurs sur la fenêtre
    window.addEventListener('mousemove', () => this.resetTimer());
    window.addEventListener('mousedown', () => this.resetTimer());
    window.addEventListener('keypress', () => this.resetTimer());
    window.addEventListener('scroll', () => this.resetTimer());
    window.addEventListener('touchmove', () => this.resetTimer());
  }

  private resetTimer() {
    if (this.timeoutId) {
      clearTimeout(this.timeoutId);
    }

    // Utilisation de NgZone pour éviter de perturber la détection de changements d'Angular
    this.ngZone.runOutsideAngular(() => {
      this.timeoutId = setTimeout(() => {
        this.ngZone.run(() => {
          this.logoutUser();
        });
      }, this.IDLE_TIMEOUT);
    });
  }

private logoutUser() {
  console.log("Inactivité détectée : déconnexion automatique en cours..."); // 👈 Ajoutez ceci
  localStorage.clear();
  sessionStorage.clear();
  this.router.navigate(['/login']);
}

  public stopWatching() {
    if (this.timeoutId) {
      clearTimeout(this.timeoutId);
    }
    // Nettoyer les écouteurs si nécessaire
  }
}