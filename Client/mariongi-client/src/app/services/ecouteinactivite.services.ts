import { Injectable, NgZone } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class ecouteinactivite {
  private timeoutId: any;
  // valeur en millisecondes
  private IDLE_TIMEOUT = 15 * 60 * 1000; 

  constructor(
    private router: Router, 
    private ngZone: NgZone,
    private http: HttpClient
  ) {}

  public startWatching() {
    // 1. On récupère la configuration de timeout depuis le backend .NET
    this.http.get<{ timeoutMinutes: number }>(`${environment.apiUrl}/configuration/session-timeout`)
      .subscribe({
        next: (res) => {
          if (res && res.timeoutMinutes) {
            // Conversion des minutes en millisecondes
            this.IDLE_TIMEOUT = res.timeoutMinutes * 60 * 1000;
          }
          this.initListeners();
        },
        error: () => {
          // En cas d'erreur de l'API, on utilise la valeur par défaut
          this.initListeners();
        }
      });
  }

  private initListeners() {
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
    localStorage.clear();
    sessionStorage.clear();
    this.router.navigate(['/login']);
  }

  public stopWatching() {
    if (this.timeoutId) {
      clearTimeout(this.timeoutId);
    }
  }
}