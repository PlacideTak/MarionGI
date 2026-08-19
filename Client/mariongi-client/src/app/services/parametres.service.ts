import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class ParametresService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/Parametres`;

  getParametres(): Observable<any> {
    return this.http.get<any>(this.apiUrl);
  }

  sauvegarderParametres(parametres: any): Observable<any> {
    return this.http.post<any>(this.apiUrl, parametres);
  }
}