import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BienDto, TypeBien, StatutBien } from '../models/gestimmo.models';
import { environment } from '../../environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class BiensService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/Biens`;

  // --- Référentiels de libellés ---
   public getStatutSeverity(statut: StatutBien): 'success' | 'info' | 'warn' | 'danger' {
    switch (statut) {
      case StatutBien.Disponible: return 'success';
      case StatutBien.Loue: return 'info';
      case StatutBien.EnTravaux: return 'danger';
      case StatutBien.Reserve: return 'warn';
      default: return 'danger';
    }
  }

public getStatutLibelleSeverity(statut: string): 'success' | 'info' | 'warn' | 'danger' {
  switch (statut) {
    case this.statutsBien[0].label: return 'info';     // Disponible
    case this.statutsBien[1].label: return 'success';  // Loué
    case this.statutsBien[2].label: return 'warn';      // Reserve
    case this.statutsBien[3].label: return 'danger';   // En travaux
    default: return 'danger';
  }
}

  readonly typesBien = [
    { label: 'Appartement', value: TypeBien.Appartement },
    { label: 'Maison', value: TypeBien.Maison },
    { label: 'Studio', value: TypeBien.Studio },
    { label: 'Terrain', value: TypeBien.Terrain },
    { label: 'Boutique', value: TypeBien.Boutique },
    { label: 'Chambre', value: TypeBien.Chambre },
    { label: 'Villa', value: TypeBien.Villa }
  ];

  readonly statutsBien = [
    { label: 'Disponible', value: StatutBien.Disponible },
    { label: 'Loué', value: StatutBien.Loue },
    { label: 'Reserve', value: StatutBien.Reserve },
    { label: 'En travaux', value: StatutBien.EnTravaux }
  ];

  getTypeLibelle(type: TypeBien): string {
    const found = this.typesBien.find(t => t.value === type);
    return found ? found.label : 'Inconnu';
  }

  getStatutLibelle(statut: StatutBien): string {
    const found = this.statutsBien.find(s => s.value === statut);
    return found ? found.label : 'Inconnu';
  }

  // --- Appels API existants ---

  // GET: /api/Biens
  getBiens(): Observable<BienDto[]> {
    return this.http.get<BienDto[]>(this.apiUrl);
  }

  // GET: /api/Biens/{id}
  getBienById(id: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  // POST: /api/Biens
  createBien(formData: FormData): Observable<BienDto> {
    return this.http.post<BienDto>(this.apiUrl, formData);
  }

  // PUT: /api/Biens/{id}
  updateBien(id: string, formData: FormData): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, formData);
  }

  // DELETE: /api/Biens/{id}
  deleteBien(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}