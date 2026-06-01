import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PriceFetchLog, PriceFetchStatus } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class PriceFetchService {
  private apiUrl = `${environment.apiUrl}/pricefetch`;

  constructor(private http: HttpClient) {}

  runFetch(): Observable<PriceFetchLog> {
    return this.http.post<PriceFetchLog>(`${this.apiUrl}/run`, {});
  }

  getLogs(): Observable<PriceFetchLog[]> {
    return this.http.get<PriceFetchLog[]>(`${this.apiUrl}/logs`);
  }

  getStatus(): Observable<PriceFetchStatus> {
    return this.http.get<PriceFetchStatus>(`${this.apiUrl}/status`);
  }
}
