import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PriceProviderStatus } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class PriceProvidersService {
  private apiUrl = `${environment.apiUrl}/priceproviders`;

  constructor(private http: HttpClient) {}

  getEodhdStatus(): Observable<PriceProviderStatus> {
    return this.http.get<PriceProviderStatus>(`${this.apiUrl}/eodhd/status`);
  }

  getEodhdConfiguration(): Observable<{ hasApiKey: boolean }> {
    return this.http.get<{ hasApiKey: boolean }>(`${this.apiUrl}/eodhd/configuration`);
  }

  saveEodhdApiKey(apiKey: string): Observable<PriceProviderStatus> {
    return this.http.post<PriceProviderStatus>(`${this.apiUrl}/eodhd/configuration`, { apiKey });
  }

  clearEodhdApiKey(): Observable<any> {
    return this.http.delete(`${this.apiUrl}/eodhd/configuration`);
  }
}
